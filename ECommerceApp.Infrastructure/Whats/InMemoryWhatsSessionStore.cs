using System.Collections.Concurrent;

namespace ECommerceApp.Infrastructure.Whats
{
    public sealed class InMemoryWhatsSessionStore : IWhatsSessionStore
    {
        private readonly ConcurrentDictionary<string, (string Value, DateTimeOffset? Expires)> _map = new();

        public string? Get(string key)
        {
            if (!_map.TryGetValue(key, out var v)) return null;
            if (v.Expires is { } exp && exp < DateTimeOffset.UtcNow)
            {
                _map.TryRemove(key, out _);
                return null;
            }
            return v.Value;
        }

        public void Set(string key, string value, DateTimeOffset? expiresAt = null)
            => _map[key] = (value, expiresAt);

        public void Remove(string key)
            => _map.TryRemove(key, out _);
    }
}
