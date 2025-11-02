// ECommerceApp.Core.Entities/Cart.cs

using System;

namespace ECommerceApp.Core.Entities
{
    public class Cart : BaseEntity 
    {

        public Guid UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}