using System.Collections.Concurrent;
using ECommerceApp.Application.Interfaces;

namespace ECommerceApp.Infrastructure.Whats
{
    /// <summary>
    /// Thread-safe in-memory store for upstream session/token.
    /// </summary>
    public class InMemoryWhatsSessionStore : IWhatsSessionStore
    {
        private readonly ConcurrentDictionary<string, (string Value, DateTimeOffset? Expires)> _map
            = new(StringComparer.Ordinal);

        public string? Get(string key)
        {
            if (!_map.TryGetValue(key, out var entry)) return null;
            if (entry.Expires is { } exp && exp < DateTimeOffset.UtcNow)
            {
                _map.TryRemove(key, out _);
                return null;
            }
            return entry.Value;
        }

        public void Set(string key, string value, DateTimeOffset? expires = null)
            => _map[key] = (value, expires);

        public void Remove(string key)
            => _map.TryRemove(key, out _);
    }
}
