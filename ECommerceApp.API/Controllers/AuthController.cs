using System.Security.Claims;
using ECommerceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtTokenService _jwt;

        public AuthController(IJwtTokenService jwt) => _jwt = jwt;

        public record LoginDto(string Name, string Password);

        [HttpPost("login")] // example local login; swap with your WhatsGPS flow if needed
        public IActionResult Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Name) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { error = "Name and Password required" });

            // TODO: validate credentials
            var isAdmin = string.Equals(dto.Name, "admin", StringComparison.OrdinalIgnoreCase);
            var jwt = _jwt.Create(dto.Name.Trim(), isAdmin ? "Admin" : "User", isAdmin ? 2 : 1);

            return Ok(new
            {
                jwt,
                user = new { username = dto.Name.Trim(), isAdmin },
                roleId = isAdmin ? 2 : 1,
                role = isAdmin ? "Admin" : "User"
            });
        }

        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Me()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = User.Identity?.Name ?? id;
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";
            var rid = int.TryParse(User.FindFirst("roleId")?.Value, out var r) ? r : (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? 2 : 1);

            return Ok(new { id, name, role, roleId = rid });
        }
    }
}
