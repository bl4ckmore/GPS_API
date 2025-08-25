using Microsoft.Extensions.Caching.Memory;
using ECommerceApp.Application.Interfaces;

namespace ECommerceApp.Infrastructure.Services
{
    public class WhatsSessionStore : IWhatsSessionStore
    {
        private readonly IMemoryCache _cache;
        public WhatsSessionStore(IMemoryCache cache) => _cache = cache;

        public void Set(string appUserId, string vendorToken, DateTimeOffset? expires = null)
        {
            _cache.Set(Key(appUserId), vendorToken, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expires ?? DateTimeOffset.UtcNow.AddHours(8)
            });
        }

        public string? Get(string appUserId)
            => _cache.TryGetValue<string>(Key(appUserId), out var tok) ? tok : null;

        public void Remove(string appUserId) => _cache.Remove(Key(appUserId));
        private static string Key(string id) => $"whats:{id}";
    }
}
