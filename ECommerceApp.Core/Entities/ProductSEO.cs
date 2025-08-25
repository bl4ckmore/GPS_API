using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities;

public class ProductSEO : BaseEntity
{
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
}
