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

    // FIX 3: Moved 'me' logic here to match front-end request GET /api/users/me (Fixes 405)
    [HttpGet("me")]
    [AllowAnonymous] // Allow any authenticated user to check their identity
    public IActionResult Me()
    {
        // Check if authentication succeeded at all
        if (!(User?.Identity?.IsAuthenticated ?? false))
            return Ok(new { id = (string?)null, username = (string?)null, role = "guest" }); // Returning 'guest' is clearer than 'user' here

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
            .Select(u => new { u.id, u.Username, u.IsAdmin, u.LastLoginAt, u.CreatedAt })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AppUser body)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.id == id);
        if (u is null) return NotFound();

        // Only toggling IsAdmin here; add more fields as needed
        u.IsAdmin = body.IsAdmin;
        u.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { u.id, u.Username, u.IsAdmin, u.LastLoginAt, u.CreatedAt, u.UpdatedAt });
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