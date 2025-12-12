namespace ECommerceApp.Core.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; set; }

    // Navigation property (Optional but recommended)
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}