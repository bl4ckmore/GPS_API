namespace ECommerceApp.Core.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; set; }
    // Remove local IsDeleted — we already inherit IsDeleted from BaseEntity
}
