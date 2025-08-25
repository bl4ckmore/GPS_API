using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities; 


public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; } = null;
    public Guid ProductId { get; set; }
    public Product? Product { get; set; } = null; 
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? CustomizationDate { get; set; }
}