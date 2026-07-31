using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using Core.Utilities.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Core.Aspects.Autofac.Caching
{
    /// <summary>
    /// Metodların çıktılarını otomatik olarak önbelleğe alır (Cache).
    /// Hem senkron hem de asenkron (Task/Task&lt;T&gt;) metodları tam olarak destekler.
    /// 
    /// Kullanım:
    ///   [CacheAspect(duration: 30)] // 30 dakika cache'ler
    ///   public async Task<IDataResult<List<Product>>> GetAll() { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CacheAspect : MethodInterception
    {
        private readonly int _duration;
        private readonly ICacheManager _cacheManager;
        private readonly ILoggerService _logger;
        private readonly bool _logCacheHits;

        public CacheAspect(int duration = 60, bool logCacheHits = true)
        {
            _duration = duration > 0 ? duration : 60;
            _cacheManager = ServiceTool.ServiceProvider.GetService<ICacheManager>() 
                ?? throw new InvalidOperationException("ICacheManager servisi çözülemedi. CoreModule yüklendiğinden emin olun.");
            _logger = ServiceTool.ServiceProvider.GetService<ILoggerService>()!;
            _logCacheHits = logCacheHits;
        }

        public override void Intercept(IInvocation invocation)
        {
            if (_cacheManager == null)
            {
                invocation.Proceed();
                return;
            }

            if (!IsAsyncMethod(invocation.Method))
            {
                HandleSync(invocation);
                return;
            }

            var returnType = invocation.Method.ReturnType;
            if (returnType == typeof(Task))
            {
                // Değer döndürmeyen async metodlar cache'lenmez, doğrudan çalıştırılır
                invocation.ReturnValue = HandleAsyncVoid(invocation);
                return;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];
                var method = typeof(CacheAspect)
                    .GetMethod(nameof(HandleAsyncGeneric), BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(resultType);

                if (method == null)
                {
                    throw new InvalidOperationException("Asenkron önbellek işleyicisi çözülemedi.");
                }

                invocation.ReturnValue = method.Invoke(this, new object[] { invocation }) ?? Task.CompletedTask;
                return;
            }

            throw new NotSupportedException($"Önbellek aspect'i {returnType} dönüş tipini desteklemiyor.");
        }

        // ─── Senkron Metod İşleyicisi ─────────────────────────────────────────

        private void HandleSync(IInvocation invocation)
        {
            var key = CacheKeyBuilder.BuildCacheKey(invocation);

            try
            {
                if (_cacheManager.IsAdd(key))
                {
                    LogCacheHit(key);
                    var cachedValue = _cacheManager.Get(key);
                    if (cachedValue != null)
                    {
                        invocation.ReturnValue = cachedValue;
                        return;
                    }
                }

                LogCacheMiss(key);
                invocation.Proceed();

                if (invocation.ReturnValue != null)
                {
                    _cacheManager.Add(key, invocation.ReturnValue, _duration);
                    LogCacheAdd(key);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Önbellek işlemi sırasında hata oluştu. Key: {CacheKey}", ex, key);
                // Önbellek hatası iş akışını bozmamalı, doğrudan DB'ye git
                if (invocation.ReturnValue == null)
                {
                    invocation.Proceed();
                }
            }
        }

        // ─── Asenkron Metod İşleyicileri ──────────────────────────────────────

        private async Task HandleAsyncVoid(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.ReturnValue is Task task)
            {
                await task.ConfigureAwait(false);
                return;
            }
            throw new InvalidOperationException("Asenkron işlem Task döndürmedi.");
        }

        private async Task<T> HandleAsyncGeneric<T>(IInvocation invocation)
        {
            var key = CacheKeyBuilder.BuildCacheKey(invocation);

            try
            {
                if (await _cacheManager.IsAddAsync(key))
                {
                    LogCacheHit(key);
                    var cachedValue = await _cacheManager.GetAsync(key);
                    if (cachedValue is T typed)
                    {
                        return typed;
                    }
                    if (cachedValue != null)
                    {
                        return (T)cachedValue;
                    }
                }

                LogCacheMiss(key);
                invocation.Proceed();

                if (invocation.ReturnValue is Task<T> task)
                {
                    var result = await task.ConfigureAwait(false);
                    if (result != null)
                    {
                        await _cacheManager.AddAsync(key, result, _duration);
                        LogCacheAdd(key);
                    }
                    return result;
                }

                throw new InvalidOperationException("Asenkron işlem Task<T> döndürmedi.");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Asenkron önbellek işlemi sırasında hata oluştu. Key: {CacheKey}", ex, key);
                throw;
            }
        }

        // ─── Loglama ─────────────────────────────────────────────────────────

        private void LogCacheHit(string key)
        {
            if (_logCacheHits)
            {
                _logger?.LogInfo("💾 [CACHE HIT] {CacheKey}", key);
            }
        }

        private void LogCacheMiss(string key)
        {
            if (_logCacheHits)
            {
                _logger?.LogInfo("🔄 [CACHE MISS] {CacheKey}", key);
            }
        }

        private void LogCacheAdd(string key)
        {
            if (_logCacheHits)
            {
                _logger?.LogInfo("✅ [CACHE ADDED] {CacheKey} ({Duration} dk)", key, _duration);
            }
        }

        private static bool IsAsyncMethod(MethodInfo method)
        {
            return method.ReturnType == typeof(Task) ||
                   (method.ReturnType.IsGenericType &&
                    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }
    }
}
