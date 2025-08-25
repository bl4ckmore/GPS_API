namespace ECommerceApp.Application.Interfaces
{
    public interface IWhatsSessionStore
    {
        void Set(string appUserId, string vendorToken, DateTimeOffset? expires = null);
        string? Get(string appUserId);
        void Remove(string appUserId);
    }
}
