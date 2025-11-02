namespace ECommerceApp.Core.Entities;

public class OrderItem : BaseEntity
{
    // BaseEntity: id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt

    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }

    public int qty { get; set; }                // keep same casing you use in CartItem
    public decimal unitPrice { get; set; }

    public string? title { get; set; }
    public string? imageUrl { get; set; }
    public string? sku { get; set; }
}
