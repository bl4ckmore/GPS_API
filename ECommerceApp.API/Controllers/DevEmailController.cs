// ecommerceapp.api/Controllers/DevEmailController.cs
using ECommerceApp.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/dev/email")]
[Authorize(Roles = "Admin")] // keep it protected
public sealed class DevEmailController : ControllerBase
{
    private readonly IEmailSender _email;
    private readonly ILogger<DevEmailController> _log;

    public DevEmailController(IEmailSender email, ILogger<DevEmailController> log)
    {
        _email = email;
        _log = log;
    }

    // GET /api/dev/email/ping?to=you@example.com
    [HttpGet("ping")]
    public async Task<IActionResult> Ping([FromQuery] string to, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(to)) return BadRequest("Provide ?to=");
        await _email.SendAsync(to, "SMTP ping", "<p>This is a test.</p>", ct);
        _log.LogInformation("Ping email requested to {To}", to);
        return Ok(new { ok = true });
    }
}
