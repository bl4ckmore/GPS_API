using System;

namespace ECommerceApp.Core.Entities
{
    public class AppUser : BaseEntity
    {
        public string Username { get; set; } = default!;
        public bool IsAdmin { get; set; }

        public string? Phone { get; set; }

        public bool Verified { get; set; } = false;


        public DateTime? LastLoginAt { get; set; }

        public string? Email { get; set; }
        public string? PasswordHash { get; set; }


    }
}
