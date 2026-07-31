using Castle.DynamicProxy;
using Core.Utilities.Interceptors;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;

namespace Core.Aspects.Autofac.Transaction
{
    /// <summary>
    /// Metodların veritabanı işlemlerini bir Transaction Scope (işlem kapsamı) içerisinde çalıştırır.
    /// Herhangi bir hata durumunda tüm işlemleri otomatik olarak geri alır (Rollback).
    /// Senkron ve asenkron (Task/Task&lt;T&gt;) metodları tam olarak destekler.
    /// 
    /// Kullanım:
    ///   [TransactionScopeAspect]
    ///   public async Task<IResult> AddProductAndRegisterHistory(...) { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TransactionScopeAspect : MethodInterception
    {
        public override void Intercept(IInvocation invocation)
        {
            if (!IsAsyncMethod(invocation.Method))
            {
                ExecuteSync(invocation);
                return;
            }

            var returnType = invocation.Method.ReturnType;
            if (returnType == typeof(Task))
            {
                invocation.ReturnValue = ExecuteAsync(invocation);
                return;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];
                var method = typeof(TransactionScopeAspect)
                    .GetMethod(nameof(ExecuteAsyncWithResult), BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(resultType);

                if (method == null)
                {
                    throw new InvalidOperationException("Asenkron transaction yöneticisi çözülemedi.");
                }

                invocation.ReturnValue = method.Invoke(this, new object[] { invocation }) ?? Task.CompletedTask;
                return;
            }

            throw new NotSupportedException($"TransactionScopeAspect {returnType} dönüş tipini desteklemiyor.");
        }

        private void ExecuteSync(IInvocation invocation)
        {
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    invocation.Proceed();
                    transactionScope.Complete();
                }
                catch
                {
                    throw;
                }
            }
        }

        private async Task ExecuteAsync(IInvocation invocation)
        {
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    invocation.Proceed();
                    var task = invocation.ReturnValue as Task ?? Task.CompletedTask;
                    await task.ConfigureAwait(false);
                    transactionScope.Complete();
                }
                catch
                {
                    throw;
                }
            }
        }

        private async Task<T> ExecuteAsyncWithResult<T>(IInvocation invocation)
        {
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    invocation.Proceed();
                    var task = invocation.ReturnValue as Task<T>;
                    if (task == null)
                    {
                        throw new InvalidOperationException("Asenkron transaction metodu Task<T> döndürmedi.");
                    }

                    var result = await task.ConfigureAwait(false);
                    transactionScope.Complete();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        private static bool IsAsyncMethod(MethodInfo methodInfo)
        {
            var returnType = methodInfo.ReturnType;
            return returnType == typeof(Task) ||
                   (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));
        }
    }
}
