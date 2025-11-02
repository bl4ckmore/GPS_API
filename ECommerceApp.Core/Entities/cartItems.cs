using System;

namespace ECommerceApp.Core.Entities
{
    // Inherits your existing BaseEntity in the same namespace.
    // Do NOT redeclare 'id', CreatedAt, etc.—they come from BaseEntity.
    public class CartItem : BaseEntity
    {
        // Foreign keys
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }

        // Business fields (camelCase to match the rest of your code)
        public int qty { get; set; } = 1;
        public decimal unitPrice { get; set; } = 0m;

        // Navigations (optional)
        public Cart? Cart { get; set; }
        public Product? Product { get; set; }
    }
}
