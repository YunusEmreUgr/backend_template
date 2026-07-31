using Castle.DynamicProxy;
using System.Reflection;

namespace Core.Utilities.Interceptors
{
    /// <summary>
    /// Autofac'ın hangi interceptor'ların hangi metodlara uygulanacağını seçmesini sağlar.
    /// 
    /// Çalışma mantığı:
    ///   1. Sınıf üzerindeki MethodInterceptionBaseAttribute attribute'larını toplar
    ///   2. Method üzerindeki MethodInterceptionBaseAttribute attribute'larını toplar
    ///   3. Hepsini Priority sırasına göre sıralar
    /// 
    /// Autofac modülünde kullanım:
    ///   builder.RegisterType&lt;MyManager&gt;()
    ///       .EnableInterfaceInterceptors(new ProxyGenerationOptions
    ///       {
    ///           Selector = new AspectInterceptorSelector()
    ///       });
    /// </summary>
    public class AspectInterceptorSelector : IInterceptorSelector
    {
        public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
        {
            // Sınıf düzeyindeki aspect'leri topla
            var classAttributes = type
                .GetCustomAttributes<MethodInterceptionBaseAttribute>(true)
                .ToList();

            // Method düzeyindeki aspect'leri topla
            var methodAttributes = type
                .GetMethod(method.Name)
                ?.GetCustomAttributes<MethodInterceptionBaseAttribute>(true)
                ?? Enumerable.Empty<MethodInterceptionBaseAttribute>();

            classAttributes.AddRange(methodAttributes);

            // Priority'ye göre sırala (düşük Priority = önce çalışır)
            return classAttributes.OrderBy(x => x.Priority).ToArray();
        }
    }
}
