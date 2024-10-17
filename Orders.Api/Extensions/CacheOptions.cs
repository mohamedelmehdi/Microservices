using Microsoft.Extensions.Caching.Distributed;

namespace Orders.Api.Extensions
{
    public static class CacheOptions
    {
        public static DistributedCacheEntryOptions DefaultExpiration => 
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
    }
}
