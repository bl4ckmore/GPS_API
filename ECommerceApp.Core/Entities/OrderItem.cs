using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities;

public class  OrderItem : BaseEntity {
    public Guid OrderId { get; set; }
    public Order? Order { get; set; } = null; 
    public Guid ProductId { get; set; }
    public Product? Product { get; set; } = null;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty; 
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public string? CustomizationData { get; set; }
    
}