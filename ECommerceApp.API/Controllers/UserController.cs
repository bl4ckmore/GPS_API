using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using System.Security.Claims; // Needed for FindFirstValue

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")] // Resolves to: /api/users
[Authorize(Roles = "Admin")] // This restriction applies to all actions *except* those marked [AllowAnonymous]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UsersController(ApplicationDbContext db) => _db = db;

    // 'me' logic stays as you had it (just returns basic identity)
    [HttpGet("me")]
    [AllowAnonymous] // currently allows anyone to call this; keep as you wrote
    public IActionResult Me()
    {
        // Check if authentication succeeded at all
        if (!(User?.Identity?.IsAuthenticated ?? false))
            return Ok(new { id = (string?)null, username = (string?)null, role = "guest" });

        // Extract user claims
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "user";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "user";

        return Ok(new { id, username = name, role });
    }

    [HttpGet] // Maps to: GET /api/users (Requires Admin role)
    public async Task<IActionResult> List()
    {
        var items = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new
            {
                u.id,
                u.Username,
                u.Email,      // NEW
                u.Phone,      // NEW
                u.IsAdmin,
                u.Verified,   // NEW
                u.LastLoginAt,
                u.CreatedAt,
                u.UpdatedAt   // NEW
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AppUser body)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.id == id);
        if (u is null) return NotFound();

        // Username update is optional – keep as-is or change if provided
        if (!string.IsNullOrWhiteSpace(body.Username) && body.Username != u.Username)
        {
            u.Username = body.Username;
        }

        // Existing behavior
        u.IsAdmin = body.IsAdmin;

        // NEW: Email / Phone / Verified updates
        u.Email = body.Email ?? u.Email;
        u.Phone = body.Phone ?? u.Phone;
        u.Verified = body.Verified;

        // Do NOT touch PasswordHash here (separate flow later)
        u.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            u.id,
            u.Username,
            u.Email,
            u.Phone,
            u.IsAdmin,
            u.Verified,
            u.LastLoginAt,
            u.CreatedAt,
            u.UpdatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.id == id);
        if (u is null) return NotFound();

        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
