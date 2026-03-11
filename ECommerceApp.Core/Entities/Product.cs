using System;
using System.Collections.Generic;

namespace ECommerceApp.Core.Entities
{
    public class Product : BaseEntity
    {
        // Required basics (ensure these already exist in your file; add if missing)
        public new Guid id { get; set; }                // keep your existing key shape
        public string Name { get; set; } = string.Empty;
        public string? SKU { get; set; }
        public string? Description { get; set; }
        public string? LongDescription { get; set; }

        // ✅ ADD these price fields so Seeder + Configuration compile
        public decimal Price { get; set; }                     // required
        public decimal? CompareAtPrice { get; set; }           // optional
        public decimal? SalePrice { get; set; }                // optional

        public int Stock { get; set; }
        public decimal? Weight { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }

        // Category
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Optional extras the seeder/config use (add if not present)
        public string? Type { get; set; }
        public string? ImageUrl { get; set; }
        public string[]? Images { get; set; }                   // mapped as text[] in config
        public string[]? Features { get; set; }                 // mapped as text[] in config
        public Dictionary<string, string>? Parameters { get; set; } // mapped as jsonb

        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }

        public string? VideoUrl { get; set; }
    }
}
