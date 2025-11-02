using System.Net;
using System.Text.Json;
using ECommerceApp.Application.Interfaces;   // IJwtTokenService, IWhatsSessionStore
using ECommerceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/auth/whatsgps")]
public class WhatsGpsAuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WhatsGpsAuthController> _logger;
    private readonly IJwtTokenService _jwt;
    private readonly IWhatsSessionStore _session;   // <-- from Application
    private readonly IConfiguration _cfg;
    private readonly ApplicationDbContext _db;

    public WhatsGpsAuthController(
        IHttpClientFactory httpFactory,
        ILogger<WhatsGpsAuthController> logger,
        IJwtTokenService jwt,
        IWhatsSessionStore session,
        IConfiguration cfg,
        ApplicationDbContext db)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _jwt = jwt;
        _session = session;
        _cfg = cfg;
        _db = db;
    }

    public sealed record LoginDto(string Name, string Password, string? Lang, int? TimeZoneSecond);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "Name and Password are required" });

        var client = _httpFactory.CreateClient("whats");
        var loginPath = _cfg["WhatsGps:LoginPath"] ?? "user/login.do";
        var method = (_cfg["WhatsGps:Method"] ?? "POST").ToUpperInvariant();
        var bodyFormat = (_cfg["WhatsGps:BodyFormat"] ?? "form").ToLowerInvariant();
        var userField = _cfg["WhatsGps:UsernameField"] ?? "name";
        var passField = _cfg["WhatsGps:PasswordField"] ?? "password";
        var tokenPaths = _cfg.GetSection("WhatsGps:TokenPaths").Get<string[]>() ?? new[] { "token", "data.token", "data.session" };

        var lang = string.IsNullOrWhiteSpace(dto.Lang) ? "en" : dto.Lang!;
        var tz = dto.TimeZoneSecond ?? (int)(-TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds);

        var fields = new Dictionary<string, string?>
        {
            [userField] = dto.Name!.Trim(),
            [passField] = dto.Password!.Trim(),
            ["lang"] = lang,
            ["timeZoneSecond"] = tz.ToString()
        };

        try
        {
            using var req = BuildRequest(method, bodyFormat, loginPath, fields);
            req.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");

            using var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return StatusCode((int)res.StatusCode, body);

            var ctype = res.Content.Headers.ContentType?.MediaType ?? "";
            if (!ctype.Contains("json", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "Upstream returned non-JSON", contentType = ctype });

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var vendorToken = ExtractTokenViaPaths(root, tokenPaths) ?? ExtractTokenFallback(root);
            if (string.IsNullOrWhiteSpace(vendorToken))
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "Token missing in upstream JSON" });

            var username = dto.Name!.Trim();

            // ensure local user exists (simplified)
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user is null)
            {
                user = new Core.Entities.AppUser
                {
                    id = Guid.NewGuid(),
                    Username = username,
                    IsAdmin = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _db.Users.AddAsync(user);
            }
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // store vendor token
            _session.Set(username, vendorToken, DateTimeOffset.UtcNow.AddHours(8));

            // local JWT
            var jwt = _jwt.Create(username, user.IsAdmin ? "Admin" : "User", user.IsAdmin ? 2 : 1);

            return Ok(new
            {
                jwt,
                user = new { user.id, user.Username, user.IsAdmin },
                roleId = user.IsAdmin ? 2 : 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsGPS login error");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Login proxy error", detail = ex.Message });
        }
    }

    // helpers
    private static HttpRequestMessage BuildRequest(string method, string bodyFormat, string path, Dictionary<string, string?> fields)
    {
        path = path.TrimStart('/');
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
        if (root.ValueKind == JsonValueKind.Object)
        {
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
        }
        return null;
    }
}
