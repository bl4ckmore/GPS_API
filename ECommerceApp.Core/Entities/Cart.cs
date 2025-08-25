using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities;


public class Cart : BaseEntity
{
    public Guid UserSessionId { get; set; }
    public UserSession? UserSession { get; set; } = null;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    public decimal TotalAmount => Items.Sum(x => x.Quantity * x.UnitPrice);
    public int TotalItems => Items.Sum(x=>x.Quantity);
}