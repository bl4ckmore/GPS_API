using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommerceApp.Core.Entities;


namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (!(User?.Identity?.IsAuthenticated ?? false))
            return Ok(new { id = (string?)null, username = (string?)null, role = "user" });

        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "user";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "user";
        return Ok(new { id, username = name, role });
    }
}
