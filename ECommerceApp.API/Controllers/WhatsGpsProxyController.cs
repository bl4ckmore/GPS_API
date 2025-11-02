using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;

// App interfaces
using ECommerceApp.Application.Interfaces; // IWhatsSessionStore

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/whatsgps/proxy")]
public class WhatsGpsProxyController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<WhatsGpsProxyController> _logger;
    private readonly IWhatsSessionStore _session;

    public WhatsGpsProxyController(
        IHttpClientFactory http,
        ILogger<WhatsGpsProxyController> logger,
        IWhatsSessionStore session)
    {
        _http = http;
        _logger = logger;
        _session = session;
    }

    // Example passthrough (adjust to your actual proxy endpoints)
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { ok = true });
}
