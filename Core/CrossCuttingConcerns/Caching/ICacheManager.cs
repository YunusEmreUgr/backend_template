namespace Core.CrossCuttingConcerns.Caching
{
    /// <summary>
    /// Cache yönetimi için soyutlama interface'i.
    /// 
    /// Bu interface sayesinde cache implementasyonu (MemoryCache, Redis, vb.)
    /// iş mantığından bağımsız olarak değiştirilebilir.
    /// 
    /// Mevcut implementasyon: MemoryCacheManager (IMemoryCache tabanlı)
    /// Redis için: RedisCacheManager eklenebilir (bu interface'i implement eder)
    /// 
    /// Servis kayıt (CoreModule):
    ///   services.AddSingleton&lt;ICacheManager, MemoryCacheManager&gt;();
    /// 
    /// CacheAspect attribute'u bu interface'i kullanır.
    /// </summary>
    public interface ICacheManager
    {
        // ─── Sync Metodlar ───────────────────────────────────────────────────

        /// <summary>Cache'den generic tip olarak okur.</summary>
        T Get<T>(string key);

        /// <summary>Cache'den object olarak okur.</summary>
        object Get(string key);

        /// <summary>Cache'e ekler. duration: dakika cinsinden TTL.</summary>
        void Add(string key, object value, int duration);

        /// <summary>Key var mı kontrol eder.</summary>
        bool IsAdd(string key);

        /// <summary>Key'i cache'den siler.</summary>
        void Remove(string key);

        /// <summary>Pattern ile eşleşen tüm key'leri siler. (ör: "Products_*")</summary>
        void RemoveByPattern(string pattern);

        // ─── Async Metodlar ──────────────────────────────────────────────────

        Task<T> GetAsync<T>(string key);
        Task<object> GetAsync(string key);
        Task AddAsync(string key, object value, int duration);
        Task<bool> IsAddAsync(string key);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);

        // ─── Toplu İşlemler ──────────────────────────────────────────────────

        /// <summary>Birden fazla pattern ile eşleşen key'leri tek seferde siler.</summary>
        Task RemoveByPatternsAsync(IEnumerable<string> patterns);

        // ─── Yardımcılar ─────────────────────────────────────────────────────

        /// <summary>
        /// Standart cache key üretir.
        /// Örnek: "ProductManager.GetAll_12_20" (method adı + argümanlar)
        /// </summary>
        string GenerateCacheKey(string methodName, params object[] args);
    }
}
