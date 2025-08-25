using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities;

public class ProductImage : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
}
