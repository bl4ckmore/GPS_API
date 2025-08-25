using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities;

public class ProductAttribute : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
