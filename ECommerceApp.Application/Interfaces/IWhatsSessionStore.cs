namespace ECommerceApp.Infrastructure.Whats
{
    public interface IWhatsSessionStore
    {
        string? Get(string key);
        void Set(string key, string value, DateTimeOffset? expiresAt = null);
        void Remove(string key);
    }
}