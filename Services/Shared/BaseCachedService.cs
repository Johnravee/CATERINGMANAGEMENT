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
    }
}
