using ECommerceApp.Core.Entities;

namespace ECommerceApp.Core.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LongDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public decimal Weight { get; set; }
        public string? Dimensions { get; set; }

        // Fix typo in CategoryId property name (was 'CategoriId')
        public Guid CategoryId { get; set; }

        // Category should be nullable or initialized properly
        public Category? Category { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Assuming ProductSEO is a class, make this nullable
        public ProductSEO? SEO { get; set; }
    }
}
