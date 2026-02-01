using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ECommerceApp.Application.DTOs.Auth;
using ECommerceApp.Application.Interfaces;
using ECommerceApp.Core.Entities;
using ECommerceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly WhatsGpsAuthController _whatsGpsAuth;

        public AuthController(
            ApplicationDbContext db,
            IJwtTokenService jwt,
            IPasswordHasher<AppUser> passwordHasher,
            WhatsGpsAuthController whatsGpsAuth)
        {
            _db = db;
            _jwt = jwt;
            _passwordHasher = passwordHasher;
            _whatsGpsAuth = whatsGpsAuth;
        }

        // ================ REGISTER (LOCAL USER) ===================

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { error = "Username, email and password are required." });
            }

            var username = dto.Username.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();

            var exists = await _db.Users.AnyAsync(u =>
                u.Username == username ||
                (u.Email != null && u.Email.ToLower() == email));

            if (exists)
            {
                return Conflict(new { error = "User with this username or email already exists." });
            }

            var user = new AppUser
            {
                id = Guid.NewGuid(),
                Username = username,
                Email = email,
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            var roleName = "User";
            var roleId = 1;
            var jwt = _jwt.Create(user.Username, roleName, roleId);

            return Ok(new
            {
                jwt,
                user = new { user.id, user.Username, user.Email, user.IsAdmin },
                roleId
            });
        }

        // ================ LOGIN (LOCAL → WHATS GPS FALLBACK) ===================

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Name) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { error = "Name and Password are required." });
            }

            var identifier = dto.Name.Trim();

            // 1) Try LOCAL login (username OR email)
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Username == identifier ||
                (u.Email != null && u.Email.ToLower() == identifier.ToLower()));

            if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
            {
                var result = _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    dto.Password
                );

                if (result == PasswordVerificationResult.Success ||
                    result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    var roleName = user.IsAdmin ? "Admin" : "User";
                    var roleId = user.IsAdmin ? 2 : 1;
                    var jwt = _jwt.Create(user.Username, roleName, roleId);

                    return Ok(new
                    {
                        jwt,
                        user = new { user.id, user.Username, user.Email, user.IsAdmin },
                        roleId
                    });
                }
            }

            // 2) Fallback: WhatsGPS login (tracking clients)
            var wgDto = new WhatsGpsAuthController.LoginDto(
                dto!.Name!,          // we already checked dto and dto.Name above
                dto.Password!,       // already checked above
                dto.Lang,
                dto.TimeZoneSecond
            );
 
            return await _whatsGpsAuth.Login(wgDto);
        }

        // ================ CURRENT USER ===================

        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Me()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = User.Identity?.Name ?? id;
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";

            var ridClaim = User.FindFirst("roleId")?.Value;
            int rid;
            if (!string.IsNullOrWhiteSpace(ridClaim) &&
                int.TryParse(ridClaim, out var parsed))
            {
                rid = parsed;
            }
            else
            {
                rid = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            }

            return Ok(new { id, name, role, roleId = rid });
        }
    }
}
