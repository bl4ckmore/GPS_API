using System.Net;
using ECommerceApp.Infrastructure.Whats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/whatsgps")]
[Authorize] // შენი JWT-ით დაცული
public sealed class WhatsGpsProxyController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    private readonly IWhatsSessionStore _store;
    private readonly ILogger<WhatsGpsProxyController> _log;

    public WhatsGpsProxyController(IHttpClientFactory http, IWhatsSessionStore store, ILogger<WhatsGpsProxyController> log)
    {
        _http = http;
        _store = store;
        _log = log;
    }

    [HttpGet("echo")]
    public async Task<IActionResult> Echo([FromQuery] string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { error = "path required" });

        var client = _http.CreateClient("whats");
        var cookie = _store.Get("whats.session");
        if (!string.IsNullOrWhiteSpace(cookie))
            client.DefaultRequestHeaders.Add("Cookie", cookie);

        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (resp.StatusCode != HttpStatusCode.OK)
        {
            _log.LogWarning("WhatsGPS proxy {Path} failed: {Status}", path, resp.StatusCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "proxy failed", upstreamStatus = (int)resp.StatusCode });
        }

        return Content(body, "application/json");
    }
}
