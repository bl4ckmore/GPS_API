using ECommerceApp.Core.Entities;

using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public class User
{
    public int Id { get; set; }

    // maps to WhatsGPS user ID (string so we can store anything)
    [Required, MaxLength(100)]
    public string WhatsGpsUserId { get; set; } = default!;

    [MaxLength(150)]
    public string? Username { get; set; }

    // "admin" | "user"
    [Required, MaxLength(20)]
    public string Role { get; set; } = "user";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
