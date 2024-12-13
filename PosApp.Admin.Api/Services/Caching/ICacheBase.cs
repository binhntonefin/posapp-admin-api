using System;

namespace PosApp.Admin.Api.Service.Caching
{
    public interface ICacheBase
    {
        T Get<T>(string key);
        void Remove(string key);
        void Add<T>(string key, T cacheData);
        T GetOrCreate<T>(string key, TimeSpan timeExpiredCache, Func<T> cacheData);
    }
}
