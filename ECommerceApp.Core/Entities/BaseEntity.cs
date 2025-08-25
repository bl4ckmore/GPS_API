using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities; 

public abstract class BaseEntity
{
    public Guid id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}