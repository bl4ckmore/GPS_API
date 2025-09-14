using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")] // RoleNormalizer makes "admin" also valid
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UsersController(ApplicationDbContext db) => _db = db;

    [HttpGet]
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
        var u = await _db.Users.FirstOrDefaultAsync(x => x.id == id); // << lower-case id
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
        var u = await _db.Users.FirstOrDefaultAsync(x => x.id == id); // << lower-case id
        if (u is null) return NotFound();

        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
