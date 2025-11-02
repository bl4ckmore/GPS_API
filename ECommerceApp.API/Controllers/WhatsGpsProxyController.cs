using System.Net;
using ECommerceApp.Application.Interfaces; // IWhatsSessionStore
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/whatsgps")]
[Authorize]
public class WhatsGpsProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IWhatsSessionStore _session; // from Application

    public WhatsGpsProxyController(IHttpClientFactory httpFactory, IWhatsSessionStore session)
    {
        _httpFactory = httpFactory;
        _session = session;
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { ok = true });

    // Add your upstream-proxy endpoints here and use _session to read vendor token/cookie
}
