using ECommerceApp.Core.Entities;

namespace ECommerceApp.Core.Entities;

public class Order : BaseEntity
{
    // BaseEntity: id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt

    public Guid UserId { get; set; }
    public OrderStatus Status { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Shipping { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    // Simple shipping/contact (optional)
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Notes { get; set; }
}
