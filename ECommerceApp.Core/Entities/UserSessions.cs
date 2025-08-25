using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities; 

public class UserSession : BaseEntity
{
    public string WhatsGpsUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; 
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; }   = string.Empty;
    public string SessionToken {  get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime LastActivityAt { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public Cart? Cart { get; set; }
}