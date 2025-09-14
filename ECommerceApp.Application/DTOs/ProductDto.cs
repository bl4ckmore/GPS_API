namespace ECommerceApp.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }

    // flags
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }

    // identity / naming
    public string Name { get; set; } = default!;
    public string? SKU { get; set; }

    // descriptions
    public string? Description { get; set; }
    public string? LongDescription { get; set; }

    // pricing
    public decimal? Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? SalePrice { get; set; }

    // inventory / physical
    public int Stock { get; set; }
    public decimal? Weight { get; set; }

    // category (both name and id for convenience)
    public int? CategoryId { get; set; }
    public string? Category { get; set; }  // name for UI
    public string? Type { get; set; }      // optional FE convenience

    // media
    public string[]? Images { get; set; }
    public string? ImageUrl { get; set; }

    // specs / features
    public string[]? Features { get; set; }
    public Dictionary<string, string>? Attributes { get; set; } // maps to Product.Parameters

    // ratings
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }

    // audit (optional)
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
