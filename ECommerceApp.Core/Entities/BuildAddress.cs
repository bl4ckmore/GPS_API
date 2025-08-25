using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities;

public class BillingAddress : BaseEntity
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}
