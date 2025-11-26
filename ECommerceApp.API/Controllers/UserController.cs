using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using System.Security.Claims;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")] // api/users
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public UsersController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ===== 1. GET CURRENT PROFILE (Fixes 405 Error) =====
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.id == userId);

        if (user == null) return NotFound();

        // Return safe DTO
        return Ok(new
        {
            user.id,
            user.Username,
            user.Email,
            user.Phone,
            user.IsAdmin,
            user.Verified,
            user.CreatedAt
        });
    }

    // ===== 2. LIST ALL USERS (Admin Only) =====
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List()
    {
        var users = await _db.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.id,
                u.Username,
                u.Email,
                u.IsAdmin,
                u.LastLoginAt,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // ===== 3. UPDATE USER (Fixes 400 Error) =====
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.id == id);
        if (user is null) return NotFound();

        // Partial Updates - only update what is sent
        if (dto.IsAdmin.HasValue) user.IsAdmin = dto.IsAdmin.Value;
        if (dto.Verified.HasValue) user.Verified = dto.Verified.Value;
        if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;
        if (!string.IsNullOrWhiteSpace(dto.Username)) user.Username = dto.Username;

        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(user);
    }

    // ===== 4. DELETE USER =====
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== HELPER: SIMPLE ME (Header info) =====
    [HttpGet("../auth/me")] // Fallback if AuthController doesn't handle it, usually AuthController handles this.
    public IActionResult MeFallback()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.Identity?.Name;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";
        return Ok(new { id, name, role });
    }
}

// DTO to allow partial updates without validation errors
public class UpdateUserDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool? IsAdmin { get; set; }
    public bool? Verified { get; set; }
}