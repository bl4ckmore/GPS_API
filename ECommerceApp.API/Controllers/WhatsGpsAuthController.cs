using System.Net;
using System.Text.Json;
using ECommerceApp.Application.Interfaces;      // IJwtTokenService
using ECommerceApp.Infrastructure.Data;        // ApplicationDbContext
using ECommerceApp.Infrastructure.Whats;       // IWhatsSessionStore
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/auth/whatsgps")]
[AllowAnonymous]
public sealed class WhatsGpsAuthController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<WhatsGpsAuthController> _log;
    private readonly IConfiguration _cfg;
    private readonly IJwtTokenService _jwt;
    private readonly IWhatsSessionStore _session;
    private readonly ApplicationDbContext _db;

    public WhatsGpsAuthController(
        IHttpClientFactory http,
        ILogger<WhatsGpsAuthController> log,
        IConfiguration cfg,
        IJwtTokenService jwt,
        IWhatsSessionStore session,
        ApplicationDbContext db)
    {
        _http = http;
        _log = log;
        _cfg = cfg;
        _jwt = jwt;
        _session = session;
        _db = db;
    }

    // DTOs
    public sealed record LoginDto(string? Name, string? Password, string? Lang, int? TimeZoneSecond);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "Name and Password are required" });

        var username = dto.Name.Trim();

        // --- Build upstream request from config ---
        var loginPath = (_cfg["WhatsGps:LoginPath"] ?? "user/login.do").TrimStart('/');
        var method = (_cfg["WhatsGps:Method"] ?? "POST").ToUpperInvariant();
        var bodyFormat = (_cfg["WhatsGps:BodyFormat"] ?? "form").ToLowerInvariant();
        var userField = _cfg["WhatsGps:UsernameField"] ?? "name";
        var passField = _cfg["WhatsGps:PasswordField"] ?? "password";
        var tokenPaths = _cfg.GetSection("WhatsGps:TokenPaths").Get<string[]>() ?? new[] { "data.token", "token", "data.session" };
        var lang = string.IsNullOrWhiteSpace(dto.Lang) ? "en" : dto.Lang!;
        var tz = dto.TimeZoneSecond ?? (int)(-TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds);

        var fields = new Dictionary<string, string?>
        {
            [userField] = username,
            [passField] = dto.Password!.Trim(),
            ["lang"] = lang,
            ["timeZoneSecond"] = tz.ToString()
        };

        using var client = _http.CreateClient("whats");
        using var req = BuildRequest(method, bodyFormat, loginPath, fields);
        req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");

        using var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        var text = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            _log.LogWarning("Upstream login failed: {Status} {Body}", (int)res.StatusCode, text);
            return Unauthorized(new { error = "Invalid username or password." });
        }

        var ctype = res.Content.Headers.ContentType?.MediaType ?? "";
        if (!ctype.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("Upstream returned non-JSON: {CType}", ctype);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Upstream returned non-JSON." });
        }

        // --- Parse JSON & validate success ---
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var root = doc.RootElement;

        // WhatsGPS sample: { "ret":1, "code":"200", "data": { ..., "token": "<JWT>" } }
        var retOk = (root.TryGetProperty("ret", out var retEl) && (retEl.ValueKind == JsonValueKind.Number && retEl.GetInt32() == 1))
                    || (root.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String && codeEl.GetString() == "200");

        var upstreamToken = ExtractTokenViaPaths(root, tokenPaths) ?? ExtractTokenFallback(root);
        if (!retOk || string.IsNullOrWhiteSpace(upstreamToken))
        {
            _log.LogWarning("Upstream did not confirm success or token missing. retOk={retOk} tokenNull={tokenNull}", retOk, string.IsNullOrWhiteSpace(upstreamToken));
            return Unauthorized(new { error = "Invalid username or password." });
        }

        // --- At this point, credentials are valid upstream. Upsert local user ---
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username, ct);
        if (user is null)
        {
            user = new ECommerceApp.Core.Entities.AppUser
            {
                id = Guid.NewGuid(),
                Username = username,
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow
            };
            await _db.Users.AddAsync(user, ct);
        }
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // --- Store upstream token (for future vendor API calls) ---
        _session.Set(username, upstreamToken!, DateTimeOffset.UtcNow.AddHours(8));

        // --- Mint local JWT tied to this username and local role ---
        var roleName = user.IsAdmin ? "Admin" : "User";
        var roleId = user.IsAdmin ? 2 : 1;
        var jwt = _jwt.Create(username, roleName, roleId);

        return Ok(new
        {
            jwt,
            roleId,
            user = new { user.id, user.Username, user.IsAdmin }
        });
    }

    // ========== helpers ==========
    private static HttpRequestMessage BuildRequest(string method, string bodyFormat, string path, Dictionary<string, string?> fields)
    {
        if (method == "GET")
            return new HttpRequestMessage(HttpMethod.Get, path + ToQueryString(fields));

        var req = new HttpRequestMessage(HttpMethod.Post, path);
        if (bodyFormat == "json")
            req.Content = new StringContent(JsonSerializer.Serialize(fields), System.Text.Encoding.UTF8, "application/json");
        else
            req.Content = new FormUrlEncodedContent(fields.Where(kv => kv.Value is not null)!);
        return req;
    }

    private static string ToQueryString(Dictionary<string, string?> kv)
    {
        if (kv == null || kv.Count == 0) return "";
        var qs = string.Join("&", kv.Where(p => p.Value is not null)
            .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));
        return "?" + qs;
    }

    private static string? ExtractTokenViaPaths(JsonElement root, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (TryGetByPath(root, parts, out var el) && el.ValueKind == JsonValueKind.String)
                return el.GetString();
        }
        return null;
    }

    private static bool TryGetByPath(JsonElement current, ReadOnlySpan<string> parts, out JsonElement found)
    {
        found = current;
        foreach (var p in parts)
            if (found.ValueKind != JsonValueKind.Object || !found.TryGetProperty(p, out found)) return false;
        return true;
    }

    private static string? ExtractTokenFallback(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in root.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.String &&
                (p.Name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(p.Name, "session", StringComparison.OrdinalIgnoreCase)))
                return p.Value.GetString();

            if (p.Value.ValueKind == JsonValueKind.Object)
            {
                var inner = ExtractTokenFallback(p.Value);
                if (!string.IsNullOrWhiteSpace(inner)) return inner;
            }
        }
        return null;
    }
}
