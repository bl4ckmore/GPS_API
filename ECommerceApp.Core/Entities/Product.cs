using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Core.Entities
{
    public class Product : BaseEntity
    {
        // Flags
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        // Identity / naming
        public string Name { get; set; } = default!;
        public string? SKU { get; set; }

        // Descriptions
        public string? Description { get; set; }
        public string? LongDescription { get; set; }

        // Pricing
        public decimal? Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? SalePrice { get; set; }

        // Inventory / physical
        public int Stock { get; set; } = 0;
        public decimal? Weight { get; set; }

        // Category (navigation, not string)
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Optional FE convenience
        public string? Type { get; set; }

        // Media
        public string[]? Images { get; set; }
        public string? ImageUrl { get; set; }

        // Specs / features
        public string[]? Features { get; set; }

        // 🔹 This is the property the alias below needs
        public Dictionary<string, string>? Parameters { get; set; }

        // Ratings
        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }

        // 🔹 Back-compat alias for old code expecting 'Attributes'
        [NotMapped]
        public Dictionary<string, string>? Attributes
        {
            get => Parameters;
            set => Parameters = value;
        }
    }
}
