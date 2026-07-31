using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Core.CrossCuttingConcerns.Caching.Redis
{
    /// <summary>
    /// Redis tabanlı dağıtık önbellek (Distributed Cache) yöneticisi.
    /// Kurumsal mimaride MemoryCache yerine, Load Balancer arkasında çalışan çoklu sunucularda
    /// senkronizasyon sağlamak için IDistributedCache (StackExchange.Redis) üzerinden implement edilmiştir.
    /// NOT: Bu sınıf şablon olarak eklenmiştir. Kullanmak için CoreModule içerisinde MemoryCacheManager yerine bunu inject edebilirsiniz.
    /// </summary>
    public class RedisCacheManager : ICacheManager
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer? _connectionMultiplexer;

        public RedisCacheManager(IDistributedCache cache, IConnectionMultiplexer? connectionMultiplexer = null)
        {
            _cache = cache;
            _connectionMultiplexer = connectionMultiplexer;
        }

        public T Get<T>(string key)
        {
            var value = _cache.GetString(key);
            if (value != null)
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            return default;
        }

        public object Get(string key)
        {
            var value = _cache.GetString(key);
            if (value != null)
            {
                return JsonSerializer.Deserialize<object>(value);
            }
            return null;
        }

        public void Add(string key, object value, int duration)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(duration)
            };
            
            var jsonValue = JsonSerializer.Serialize(value);
            _cache.SetString(key, jsonValue, options);
        }

        public bool IsAdd(string key)
        {
            return _cache.GetString(key) != null;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void RemoveByPattern(string pattern)
        {
            if (_connectionMultiplexer == null)
            {
                throw new InvalidOperationException("Redis key pattern temizleme işlemi için IConnectionMultiplexer servisinin DI container'a kaydedilmiş olması gerekir.");
            }

            var endpoints = _connectionMultiplexer.GetEndPoints();
            var db = _connectionMultiplexer.GetDatabase();
            foreach (var endpoint in endpoints)
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                var redisPattern = $"*{pattern}*";
                var keys = server.Keys(pattern: redisPattern).ToArray();
                foreach (var key in keys)
                {
                    db.KeyDelete(key);
                }
            }
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var value = await _cache.GetStringAsync(key);
            if (value != null)
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            return default;
        }

        public async Task<object> GetAsync(string key)
        {
            var value = await _cache.GetStringAsync(key);
            if (value != null)
            {
                return JsonSerializer.Deserialize<object>(value);
            }
            return null;
        }

        public async Task AddAsync(string key, object value, int duration)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(duration)
            };
            
            var jsonValue = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, jsonValue, options);
        }

        public async Task<bool> IsAddAsync(string key)
        {
            return await _cache.GetStringAsync(key) != null;
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            if (_connectionMultiplexer == null)
            {
                throw new InvalidOperationException("Redis key pattern temizleme işlemi için IConnectionMultiplexer servisinin DI container'a kaydedilmiş olması gerekir.");
            }

            var endpoints = _connectionMultiplexer.GetEndPoints();
            var db = _connectionMultiplexer.GetDatabase();
            foreach (var endpoint in endpoints)
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                var redisPattern = $"*{pattern}*";
                await foreach (var key in server.KeysAsync(pattern: redisPattern))
                {
                    await db.KeyDeleteAsync(key);
                }
            }
        }

        public async Task RemoveByPatternsAsync(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                await RemoveByPatternAsync(pattern);
            }
        }

        public string GenerateCacheKey(string methodName, params object[] args)
        {
            var sb = new StringBuilder();
            sb.Append(methodName);
            if (args != null && args.Length > 0)
            {
                sb.Append("(");
                sb.Append(string.Join(",", args.Select(a => a?.ToString() ?? "null")));
                sb.Append(")");
            }
            return sb.ToString();
        }
    }
}
