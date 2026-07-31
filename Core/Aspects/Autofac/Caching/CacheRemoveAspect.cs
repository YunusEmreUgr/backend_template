using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using Core.Utilities.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Core.Aspects.Autofac.Caching
{
    /// <summary>
    /// Metod başarıyla tamamlandığında belirlenen pattern(ler) ile eşleşen cache girdilerini siler.
    /// Genellikle ekleme, güncelleme ve silme işlemlerinde cache tutarlılığını sağlamak için kullanılır.
    /// 
    /// Kullanım:
    ///   [CacheRemoveAspect("IProductService.Get")] // IProductService.Get ile başlayan tüm cache'leri siler
    ///   public async Task<IResult> Add(Product product) { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CacheRemoveAspect : MethodInterception
    {
        private readonly string[] _patterns;
        private readonly bool _removeAll;
        private readonly ICacheManager _cacheManager;
        private readonly ILoggerService _logger;

        public CacheRemoveAspect(params string[] patterns)
        {
            _patterns = patterns ?? Array.Empty<string>();
            _removeAll = _patterns.Length == 0;
            _cacheManager = ServiceTool.ServiceProvider.GetService<ICacheManager>()
                ?? throw new InvalidOperationException("ICacheManager servisi çözülemedi.");
            _logger = ServiceTool.ServiceProvider.GetService<ILoggerService>()!;
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
                InterceptSync(invocation);
                return;
            }

            var returnType = invocation.Method.ReturnType;
            if (returnType == typeof(Task))
            {
                invocation.ReturnValue = InterceptAsyncVoid(invocation);
                return;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];
                var method = typeof(CacheRemoveAspect)
                    .GetMethod(nameof(InterceptAsyncWithResult), BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(resultType);

                if (method == null)
                {
                    throw new InvalidOperationException("Asenkron önbellek silme işleyicisi çözülemedi.");
                }

                invocation.ReturnValue = method.Invoke(this, new object[] { invocation }) ?? Task.CompletedTask;
                return;
            }

            throw new NotSupportedException($"CacheRemoveAspect {returnType} dönüş tipini desteklemiyor.");
        }

        private void InterceptSync(IInvocation invocation)
        {
            try
            {
                invocation.Proceed();
                RemoveCacheEntries(invocation).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Önbellek temizleme sırasında hata oluştu", ex);
                throw;
            }
        }

        private async Task InterceptAsyncVoid(IInvocation invocation)
        {
            try
            {
                invocation.Proceed();
                var task = (Task)invocation.ReturnValue;
                await task.ConfigureAwait(false);
                await RemoveCacheEntries(invocation);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Asenkron önbellek temizleme sırasında hata oluştu", ex);
                throw;
            }
        }

        private async Task<T> InterceptAsyncWithResult<T>(IInvocation invocation)
        {
            try
            {
                invocation.Proceed();
                var task = invocation.ReturnValue as Task<T>;
                if (task == null)
                {
                    throw new InvalidOperationException("Asenkron işlem Task<T> döndürmedi.");
                }

                var result = await task.ConfigureAwait(false);
                await RemoveCacheEntries(invocation);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError("Asenkron önbellek temizleme sırasında hata oluştu", ex);
                throw;
            }
        }

        private async Task RemoveCacheEntries(IInvocation invocation)
        {
            if (_removeAll)
            {
                // Parametresiz çağrıldıysa tüm cache temizlenir
                await _cacheManager.RemoveByPatternAsync(".*");
                _logger?.LogInfo("🧹 [CACHE REMOVED] Tüm önbellek temizlendi.");
                return;
            }

            var patterns = BuildPatterns(invocation).ToList();
            if (patterns.Count > 0)
            {
                await _cacheManager.RemoveByPatternsAsync(patterns);
                _logger?.LogInfo("🧹 [CACHE REMOVED] Önbellek silme tamamlandı. Temizlenen Desen Sayısı: {Count}", patterns.Count);
            }
        }

        private IEnumerable<string> BuildPatterns(IInvocation invocation)
        {
            var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Kullanıcı tarafından belirtilen özel pattern'ler
            foreach (var pattern in _patterns.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                patterns.Add(ToContainsRegex(pattern));
                patterns.Add(ToPrefixRegex($"{pattern}."));
                patterns.Add(ToPrefixRegex($"{pattern}:"));
            }

            // Metod ismine göre otomatik pattern üretme (örn: IProductService)
            var methodPrefix = CacheKeyBuilder.BuildMethodPrefix(invocation);
            if (!string.IsNullOrWhiteSpace(methodPrefix))
            {
                var lastDotIndex = methodPrefix.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var typePrefix = methodPrefix.Substring(0, lastDotIndex);
                    patterns.Add(ToContainsRegex(typePrefix));
                }
            }

            return patterns;
        }

        private static string ToPrefixRegex(string value)
        {
            if (string.IsNullOrEmpty(value)) return ".*";
            var escaped = Regex.Escape(value);
            return $"^{escaped}.*";
        }

        private static string ToContainsRegex(string value)
        {
            if (string.IsNullOrEmpty(value)) return ".*";
            var escaped = Regex.Escape(value);
            return $".*{escaped}.*";
        }

        private static bool IsAsyncMethod(MethodInfo method)
        {
            return method.ReturnType == typeof(Task) ||
                   (method.ReturnType.IsGenericType &&
                    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }
    }
}
