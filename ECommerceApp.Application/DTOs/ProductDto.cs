using ECommerceApp.Application.DTOs;

namespace ECommerceApp.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LongDescription { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal Weight { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }

    public Guid CategoryId { get; set; }
    public CategoryDto? Category { get; set; }

    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductAttributeDto> Attributes { get; set; } = new();
}
