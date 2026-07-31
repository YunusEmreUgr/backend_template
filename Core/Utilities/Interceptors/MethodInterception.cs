using Castle.DynamicProxy;
using System.Reflection;

namespace Core.Utilities.Interceptors
{
    /// <summary>
    /// Aspect'lerin async metodları doğru handle etmesini sağlayan temel interceptor sınıfı.
    /// 
    /// Sorun: Castle DynamicProxy'nin varsayılan intercept mekanizması async metodları
    /// (Task/Task&lt;T&gt; döndüren) doğru handle edemez. await edilen kısım intercept dışında kalır.
    /// 
    /// Çözüm: Bu sınıf sync/async metodları ayırt eder ve async metodlar için
    /// await eden wrapper'lar kullanır.
    /// 
    /// Aspect sınıfları bu sınıfı miras alır:
    ///   public class SecuredOperation : MethodInterception { ... }
    ///   public class CacheAspect : MethodInterception { ... }
    /// 
    /// Hook noktaları (override edilebilir):
    ///   OnBefore → Method çağrılmadan ÖNCE (yetki kontrolü, validasyon için)
    ///   OnAfter  → Method bittikten SONRA (kaynak temizleme için)
    ///   OnSuccess → Method başarıyla tamamlandığında (cache invalidation için)
    ///   OnException → Hata fırlatıldığında (loglama, rollback için)
    /// </summary>
    public abstract class MethodInterception : MethodInterceptionBaseAttribute
    {
        protected virtual void OnBefore(IInvocation invocation) { }
        protected virtual void OnAfter(IInvocation invocation) { }
        protected virtual void OnException(IInvocation invocation, Exception e) { }
        protected virtual void OnSuccess(IInvocation invocation) { }

        public override void Intercept(IInvocation invocation)
        {
            if (!IsAsyncMethod(invocation.Method))
            {
                // Sync method → Doğrudan intercept et
                InterceptSync(invocation);
            }
            else
            {
                // Async method → Task-aware intercept kullan
                invocation.ReturnValue = InterceptAsync(invocation);
            }
        }

        // ─── Sync Interceptor ─────────────────────────────────────────────────

        private void InterceptSync(IInvocation invocation)
        {
            OnBefore(invocation);
            try
            {
                invocation.Proceed();
                OnSuccess(invocation);
            }
            catch (Exception e)
            {
                OnException(invocation, e);
                throw; // Exception'ı yeniden fırlat
            }
            finally
            {
                OnAfter(invocation);
            }
        }

        // ─── Async Interceptor ────────────────────────────────────────────────

        private object InterceptAsync(IInvocation invocation)
        {
            var returnType = invocation.Method.ReturnType;

            if (returnType == typeof(Task))
            {
                // async void → Task döndüren method
                return InterceptAsyncTask(invocation);
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // async Task<T> → Değer döndüren async method
                var resultType = returnType.GetGenericArguments()[0];
                var method = typeof(MethodInterception)
                    .GetMethod(nameof(InterceptAsyncTaskWithResult), BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(resultType);

                if (method == null)
                    throw new InvalidOperationException("Async interception handler oluşturulamadı.");

                return method.Invoke(this, new object[] { invocation }) ?? Task.CompletedTask;
            }

            throw new NotSupportedException($"Async intercept desteklenmiyor: {returnType}");
        }

        private async Task InterceptAsyncTask(IInvocation invocation)
        {
            OnBefore(invocation);
            try
            {
                invocation.Proceed();
                var task = invocation.ReturnValue as Task ?? Task.CompletedTask;
                await task.ConfigureAwait(false);
                OnSuccess(invocation);
            }
            catch (Exception e)
            {
                OnException(invocation, e);
                throw;
            }
            finally
            {
                OnAfter(invocation);
            }
        }

        private async Task<T> InterceptAsyncTaskWithResult<T>(IInvocation invocation)
        {
            OnBefore(invocation);
            try
            {
                invocation.Proceed();
                var task = invocation.ReturnValue as Task<T>
                    ?? throw new InvalidOperationException("Method Task<T> dönmedi.");
                var result = await task.ConfigureAwait(false);
                OnSuccess(invocation);
                return result;
            }
            catch (Exception e)
            {
                OnException(invocation, e);
                throw;
            }
            finally
            {
                OnAfter(invocation);
            }
        }

        // ─── Yardımcılar ─────────────────────────────────────────────────────

        private static bool IsAsyncMethod(MethodInfo methodInfo)
        {
            var returnType = methodInfo.ReturnType;
            return returnType == typeof(Task)
                || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));
        }
    }
}
