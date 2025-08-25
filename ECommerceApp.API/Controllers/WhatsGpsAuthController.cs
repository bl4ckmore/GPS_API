using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using ECommerceApp.Application.DTOs.Auth;
using ECommerceApp.Application.Interfaces;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/auth/whatsgps")]
public class WhatsGpsAuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WhatsGpsAuthController> _logger;
    private readonly IJwtTokenService _jwt;
    private readonly IWhatsSessionStore _session;
    private readonly IConfiguration _cfg;

    public WhatsGpsAuthController(
        IHttpClientFactory httpFactory,
        ILogger<WhatsGpsAuthController> logger,
        IJwtTokenService jwt,
        IWhatsSessionStore session,
        IConfiguration cfg)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _jwt = jwt;
        _session = session;
        _cfg = cfg;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var client = _httpFactory.CreateClient("whats");

        var lang = string.IsNullOrWhiteSpace(dto.Lang) ? "en" : dto.Lang!;
        var tz = dto.TimeZoneSecond ?? (int)(-TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds);

        var query = new Dictionary<string, string?>
        {
            ["name"] = dto.Name ?? "",
            ["password"] = dto.Password ?? "",
            ["lang"] = lang,
            ["timeZoneSecond"] = tz.ToString()
        };

        var url = "user/login.do" + ToQueryString(query);

        try
        {
            using var res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var body = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var vendorToken = ExtractToken(root);
            if (string.IsNullOrWhiteSpace(vendorToken))
                return StatusCode((int)res.StatusCode, body); // pass through error

            var username = (dto.Name ?? "").Trim();

            // role assignment from config list
            var admins = _cfg.GetSection("Auth:AdminUsers").Get<string[]>() ?? Array.Empty<string>();
            var roleId = admins.Any(a => string.Equals(a?.Trim(), username, StringComparison.OrdinalIgnoreCase)) ? 2 : 1;
            var roleName = roleId == 2 ? "Admin" : "User";

            // store vendor token by username
            _session.Set(username, vendorToken);

            // mint our JWT
            var jwt = _jwt.Create(username, roleName, roleId);

            var user = ExtractUser(root) ?? new { name = username, roleId };

            return Ok(new { jwt, user, roleId, role = roleName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsGPS login failed");
            return Problem("Login proxy error: " + ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var name = User.Identity?.Name;
        var roleId = User.Claims.FirstOrDefault(c => c.Type == "roleId")?.Value ?? "1";
        return Ok(new { name, roleId = int.Parse(roleId) });
    }

    [HttpPost("logout")]
    public IActionResult Logout([FromBody] Dictionary<string, string>? body)
    {
        var username = User.Identity?.Name ?? body?.GetValueOrDefault("username");
        if (!string.IsNullOrWhiteSpace(username))
            _session.Remove(username);
        return Ok(new { ok = true });
    }

    private static string ToQueryString(Dictionary<string, string?> kv)
    {
        if (kv == null || kv.Count == 0) return "";
        var qs = string.Join("&", kv.Where(p => p.Value is not null)
            .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));
        return "?" + qs;
    }

    private static string? ExtractToken(JsonElement root)
    {
        if (root.TryGetProperty("token", out var t) && t.ValueKind == JsonValueKind.String) return t.GetString();
        if (root.TryGetProperty("data", out var d))
        {
            if (d.TryGetProperty("token", out var dt) && dt.ValueKind == JsonValueKind.String) return dt.GetString();
            if (d.TryGetProperty("session", out var ds) && ds.ValueKind == JsonValueKind.String) return ds.GetString();
        }
        foreach (var p in root.EnumerateObject())
        {
            if (p.Name.ToLower().Contains("token") && p.Value.ValueKind == JsonValueKind.String)
                return p.Value.GetString();
            if (p.Value.ValueKind == JsonValueKind.Object)
            {
                var inner = ExtractToken(p.Value);
                if (!string.IsNullOrWhiteSpace(inner)) return inner;
            }
        }
        return null;
    }

    private static object? ExtractUser(JsonElement root)
    {
        if (root.TryGetProperty("data", out var d))
        {
            if (d.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.Object)
                return JsonSerializer.Deserialize<object>(u.GetRawText());
            return JsonSerializer.Deserialize<object>(d.GetRawText());
        }
        if (root.TryGetProperty("user", out var u2) && u2.ValueKind == JsonValueKind.Object)
            return JsonSerializer.Deserialize<object>(u2.GetRawText());
        return null;
    }
}
