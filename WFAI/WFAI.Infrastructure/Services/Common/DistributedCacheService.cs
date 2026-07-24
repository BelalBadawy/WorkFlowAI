using WFAI.Application.Dtos.Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Services.Common
{
    public class DistributedCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly CacheConfiguration _cacheConfig;

        public DistributedCacheService(
            IDistributedCache cache,
            IOptions<CacheConfiguration> cacheConfig)
        {
            _cache = cache;
            _cacheConfig = cacheConfig.Value;

            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
        }

        public bool TryGet<T>(string cacheKey, out T value)
        {
            var cachedData = _cache.Get(cacheKey);

            if (cachedData is null)
            {
                value = default!;
                return false;
            }

            value = JsonSerializer.Deserialize<T>(
                Encoding.UTF8.GetString(cachedData),
                _serializerOptions)!;

            return true;
        }

        public T Set<T>(string cacheKey, T value)
        {
            var serializedData = JsonSerializer.Serialize(value, _serializerOptions);
            var bytes = Encoding.UTF8.GetBytes(serializedData);

            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(
                    _cacheConfig.SlidingExpirationInMinutes)
            };

            _cache.Set(cacheKey, bytes, options);

            return value;
        }

        public void Remove(string cacheKey)
        {
            _cache.Remove(cacheKey);
        }
    }
}