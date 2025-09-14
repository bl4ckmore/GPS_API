using System;

namespace ECommerceApp.Core.Entities
{
    public class AppUser : BaseEntity
    {
        public string Username { get; set; } = default!;
        public bool IsAdmin { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
