// ECommerceApp.Core.Entities/CartItem.cs (Example structure)

using System;

namespace ECommerceApp.Core.Entities
{
    // FIX: Inherit from BaseEntity
    public class CartItem : BaseEntity // <--- ADD THIS INHERITANCE
    {
        // public Guid id { get; set; } // May need to remove if BaseEntity provides it

        public Guid CartId { get; set; }
        public Cart Cart { get; set; }

        public Guid ProductId { get; set; }
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}