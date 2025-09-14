using System;

namespace ECommerceApp.Core.Entities
{
    public class UserLogin : BaseEntity
    {
        public Guid? UserId { get; set; }
        public string Username { get; set; } = default!;
        public string Provider { get; set; } = "WhatsGPS";
        public bool Succeeded { get; set; } = true;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
