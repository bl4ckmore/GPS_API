using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities; 

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserSessionId { get; set; } 
    public UserSession? UserSession { get; set; } = null;
    public OrderStatus Status { get; set; } = OrderStatus.Pending; 
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public BillingAddress? BillingAddress { get; set; } = null;
    public ShippingAddress? ShippingAddress { get; set; } = null;
    public PaymentInfo? PaymentInfo { get; set; }
    public string? Notes { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? TrackingNumber { get; set; }
}

public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Refunded = 6,
}