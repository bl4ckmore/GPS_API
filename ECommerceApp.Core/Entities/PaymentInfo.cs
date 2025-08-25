using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities;

public class PaymentInfo : BaseEntity
{
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
}
