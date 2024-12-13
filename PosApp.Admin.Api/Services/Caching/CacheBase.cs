using Microsoft.Extensions.Caching.Memory;

namespace PosApp.Admin.Api.Service.Caching
{
    public class CacheBase : ICacheBase
    {
        private readonly IMemoryCache _memoryCache;

        public T Get<T>(string key)
        {
            return _memoryCache.Get<T>(key);
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }

        public CacheBase(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void Add<T>(string key, T cacheData)
        {
            T cacheExisted;
            if (!_memoryCache.TryGetValue(key, out cacheExisted))
            {
                cacheExisted = cacheData;

                var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(300));

                _memoryCache.Set(cacheExisted, cacheOptions);
            }
        }

        public T GetOrCreate<T>(string key, TimeSpan timeExpiredCache, Func<T> cacheData)
        {
            return _memoryCache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = timeExpiredCache;
                return cacheData.Invoke();
            });
        }
    }

    public class CacheItem<T>
    {
        public CacheItem(T value, TimeSpan expiresAfter)
        {
            Value = value;
            ExpiresAfter = expiresAfter;
        }
        public T Value { get; }
        internal TimeSpan ExpiresAfter { get; }
        internal DateTimeOffset Created { get; } = DateTimeOffset.Now;
    }

    public class Cache<TKey, TValue>
    {
        private readonly Dictionary<TKey, CacheItem<TValue>> _cache = new Dictionary<TKey, CacheItem<TValue>>();

        public void Clear()
        {
            _cache.Clear();
        }

        public TValue Get(TKey key)
        {
            if (!_cache.ContainsKey(key)) return default(TValue);
            var cached = _cache[key];
            if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
            {
                _cache.Remove(key);
                return default;
            }
            return cached.Value;
        }

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            _cache[key] = new CacheItem<TValue>(value, expiresAfter);
        }
    }
}
