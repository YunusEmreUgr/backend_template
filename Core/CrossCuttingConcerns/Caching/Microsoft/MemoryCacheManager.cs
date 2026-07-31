using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Core.CrossCuttingConcerns.Caching.Microsoft
{
    /// <summary>
    /// ICacheManager'ın IMemoryCache (in-process) tabanlı implementasyonu.
    /// 
    /// Özellikler:
    ///   - Tek instance (Singleton) olarak kayıt edilir
    ///   - Thread-safe key takibi (_cacheKeys ConcurrentDictionary)
    ///   - Pattern-based cache temizleme (Regex ile)
    ///   - Async metodlar Task.FromResult ile sarılmış
    /// 
    /// ⚠️ YATAY ÖLÇEKLEME NOTU:
    /// Bu implementasyon yalnızca tek bir sunucu üzerinde çalışır.
    /// Birden fazla sunucu (load balancer arkasında) için Redis kullanın.
    /// 
    /// Redis geçişi: IRedisCacheManager : ICacheManager ekleyin.
    /// CoreModule'de servisi değiştirin.
    /// </summary>
    public class MemoryCacheManager : ICacheManager
    {
        private readonly IMemoryCache _memoryCache;

        // Thread-safe key takibi (pattern ile silmek için)
        private readonly HashSet<string> _cacheKeys = new();
        private readonly object _lock = new();

        public MemoryCacheManager(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        // ─── Sync Metodlar ───────────────────────────────────────────────────

        public T Get<T>(string key) => _memoryCache.Get<T>(key)!;
        public object Get(string key) => _memoryCache.Get(key)!;

        public void Add(string key, object value, int duration)
        {
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(duration));
            lock (_lock) { _cacheKeys.Add(key); }
        }

        public bool IsAdd(string key) => _memoryCache.TryGetValue(key, out _);

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            lock (_lock) { _cacheKeys.Remove(key); }
        }

        public void RemoveByPattern(string pattern)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            List<string> keysToRemove;

            lock (_lock)
            {
                keysToRemove = _cacheKeys.Where(k => regex.IsMatch(k)).ToList();
                foreach (var key in keysToRemove)
                    _cacheKeys.Remove(key);
            }

            foreach (var key in keysToRemove)
                _memoryCache.Remove(key);
        }

        // ─── Async Metodlar (MemoryCache sync olduğundan Task.FromResult ile sarılır) ─

        public Task<T> GetAsync<T>(string key) => Task.FromResult(Get<T>(key));
        public Task<object> GetAsync(string key) => Task.FromResult(Get(key));

        public Task AddAsync(string key, object value, int duration)
        {
            Add(key, value, duration);
            return Task.CompletedTask;
        }

        public Task<bool> IsAddAsync(string key) => Task.FromResult(IsAdd(key));

        public Task RemoveAsync(string key)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            RemoveByPattern(pattern);
            return Task.CompletedTask;
        }

        public async Task RemoveByPatternsAsync(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
                await RemoveByPatternAsync(pattern);
        }

        // ─── Yardımcılar ─────────────────────────────────────────────────────

        public string GenerateCacheKey(string methodName, params object[] args)
        {
            var argsString = args != null && args.Length > 0
                ? "_" + string.Join("_", args.Select(a => a?.ToString() ?? "null"))
                : string.Empty;

            return $"{methodName}{argsString}";
        }
    }
}
