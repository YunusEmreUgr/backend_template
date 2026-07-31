using Core.Utilities.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Extensions
{
    /// <summary>
    /// IServiceCollection için Core katmanı extension metodları.
    /// Program.cs'te Core modüllerini yüklemek için kullanılır.
    /// 
    /// Kullanım:
    ///   services.AddDependencyResolvers(new ICoreModule[] { new CoreModule() });
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Verilen Core modüllerini IServiceCollection'a yükler.
        /// Her modül kendi bağımlılıklarını ekler.
        /// 
        /// ÖNEMLI: ServiceTool.Create() burada çağrılmaz.
        /// Çünkü IServiceProvider bu noktada henüz oluşmadı.
        /// ServiceTool.Create(app.Services) → app.Build()'den SONRA çağrılmalı.
        /// </summary>
        public static IServiceCollection AddDependencyResolvers(
            this IServiceCollection serviceCollection,
            ICoreModule[] modules)
        {
            foreach (var module in modules)
            {
                module.Load(serviceCollection);
            }

            return serviceCollection;
        }
    }
}
