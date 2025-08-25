using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using ECommerceApp.Application.DTOs.WhatsGps;
using ECommerceApp.Application.Interfaces;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Authorize] // require our JWT for vendor calls
[Route("api/whatsgps")]
public class WhatsGpsProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WhatsGpsProxyController> _logger;
    private readonly IWhatsSessionStore _session;

    public WhatsGpsProxyController(
        IHttpClientFactory httpFactory,
        ILogger<WhatsGpsProxyController> logger,
        IWhatsSessionStore session)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _session = session;
    }

    [HttpPost("proxy")]
    public async Task<IActionResult> Proxy([FromBody] ProxyDto payload)
    {
        if (string.IsNullOrWhiteSpace(payload?.Path))
            return BadRequest(new { error = "Path is required" });

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(sub)) return Unauthorized(new { error = "Invalid JWT" });

        var vendorToken = _session.Get(sub);
        if (string.IsNullOrWhiteSpace(vendorToken))
            return Unauthorized(new { error = "Vendor session expired" });

        var client = _httpFactory.CreateClient("whats");

        var path = payload.Path.TrimStart('/');
        var @params = payload.Params ?? new Dictionary<string, string?>();
        @params["token"] = vendorToken; // inject token

        var url = path + ToQueryString(@params);

        try
        {
            using var res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var body = await res.Content.ReadAsStringAsync();
            return new ContentResult
            {
                StatusCode = (int)res.StatusCode,
                ContentType = "application/json",
                Content = body
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsGPS proxy failed");
            return Problem("WhatsGPS proxy error: " + ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static string ToQueryString(Dictionary<string, string?> kv)
    {
        if (kv == null || kv.Count == 0) return "";
        var qs = string.Join("&", kv.Where(p => p.Value is not null)
            .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));
        return "?" + qs;
    }
}
