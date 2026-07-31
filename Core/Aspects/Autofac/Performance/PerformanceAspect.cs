using Castle.DynamicProxy;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using Core.Utilities.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Core.Aspects.Autofac.Performance
{
    /// <summary>
    /// Metodların çalışma süresini izler ve belirtilen limiti (saniye) aşması durumunda
    /// ILoggerService aracılığıyla performans uyarısı (warning) loglar.
    /// Senkron ve asenkron (Task/Task&lt;T&gt;) metodları tam olarak destekler.
    /// 
    /// Kullanım:
    ///   [PerformanceAspect(interval: 5)] // Metod 5 saniyeden uzun sürerse uyar
    ///   public async Task<IDataResult<List<Product>>> GetExpensiveData() { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PerformanceAspect : MethodInterception
    {
        private readonly int _interval;
        private readonly ILoggerService _logger;

        public PerformanceAspect(int interval = 5)
        {
            _interval = interval;
            _logger = ServiceTool.ServiceProvider.GetService<ILoggerService>() 
                ?? throw new InvalidOperationException("ILoggerService servisi çözülemedi. CoreModule yüklendiğinden emin olun.");
        }

        public override void Intercept(IInvocation invocation)
        {
            var stopwatch = Stopwatch.StartNew();

            if (!IsAsyncMethod(invocation.Method))
            {
                try
                {
                    invocation.Proceed();
                }
                finally
                {
                    stopwatch.Stop();
                    LogIfLimitExceeded(invocation.Method, stopwatch.Elapsed.TotalSeconds);
                }
                return;
            }

            var returnType = invocation.Method.ReturnType;
            if (returnType == typeof(Task))
            {
                invocation.ReturnValue = InterceptAsync(invocation, stopwatch);
                return;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];
                var method = typeof(PerformanceAspect)
                    .GetMethod(nameof(InterceptAsyncWithResult), BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(resultType);

                if (method == null)
                {
                    throw new InvalidOperationException("Asenkron performans izleyicisi çözülemedi.");
                }

                invocation.ReturnValue = method.Invoke(this, new object[] { invocation, stopwatch }) ?? Task.CompletedTask;
                return;
            }

            // Fallback
            invocation.Proceed();
        }

        private async Task InterceptAsync(IInvocation invocation, Stopwatch stopwatch)
        {
            try
            {
                invocation.Proceed();
                var task = invocation.ReturnValue as Task ?? Task.CompletedTask;
                await task.ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();
                LogIfLimitExceeded(invocation.Method, stopwatch.Elapsed.TotalSeconds);
            }
        }

        private async Task<T> InterceptAsyncWithResult<T>(IInvocation invocation, Stopwatch stopwatch)
        {
            try
            {
                invocation.Proceed();
                var task = invocation.ReturnValue as Task<T>;
                if (task == null)
                {
                    throw new InvalidOperationException("Asenkron metot Task<T> döndürmedi.");
                }
                return await task.ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();
                LogIfLimitExceeded(invocation.Method, stopwatch.Elapsed.TotalSeconds);
            }
        }

        private void LogIfLimitExceeded(MethodInfo method, double elapsedSeconds)
        {
            if (elapsedSeconds > _interval)
            {
                var methodName = $"{method.DeclaringType?.Name}.{method.Name}";
                _logger?.LogWarning($"⚠️ [PERFORMANCE WARNING] {methodName} took {elapsedSeconds:0.##} seconds. Limit is {_interval} seconds.");
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
