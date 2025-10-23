using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace CATERINGMANAGEMENT.Services.Shared
{
    public abstract class BaseCachedService
    {
        protected readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

        // Track keys for invalidation
        private readonly HashSet<string> _cacheKeys = new();

        protected BaseCachedService()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        protected void AddCacheKey(string key)
        {
            _cacheKeys.Add(key);
        }

        protected void SetCache<T>(string key, T data)
        {
            _cache.Set(key, data, _cacheDuration);
            AddCacheKey(key);
        }

        protected bool TryGetCache<T>(string key, out T? data)
        {
            return _cache.TryGetValue(key, out data);
        }

        protected void InvalidateCache(params string[] additionalKeys)
        {
            foreach (var key in _cacheKeys)
                _cache.Remove(key);

            foreach (var key in additionalKeys)
                _cache.Remove(key);

            _cacheKeys.Clear();
        }

        protected void InvalidateCacheByPrefix(string prefix)
        {
            // Prepare a list to hold keys that match the prefix
            var keysToRemove = new List<string>();

            // 1️⃣ Loop through all tracked cache keys
            foreach (var key in _cacheKeys)
            {
                // 2️⃣ Check if the key starts with the prefix we passed
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    keysToRemove.Add(key);
            }

            // 3️⃣ Remove all matching keys from the cache and from the tracker
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);      // removes from IMemoryCache
                _cacheKeys.Remove(key);  // removes from our key tracker
            }
        }
    }
}
