namespace ECommerceApp.Application.Interfaces;

public interface IWhatsSessionStore
{
    string? Get(string key);
    void Set(string key, string value, DateTimeOffset? expires = null);
    void Remove(string key);
}
