using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Caching.Microsoft;
using Core.Utilities.Exceptions;
using Core.Utilities.IoC;
using Core.Utilities.Security.UserContext;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Core.DependencyResolvers
{
    /// <summary>
    /// Core katmanının temel bağımlılıklarını yükleyen modül.
    /// 
    /// Program.cs'te kullanım:
    ///   services.AddDependencyResolvers(new ICoreModule[] { new CoreModule() });
    /// 
    /// Yüklenen servisler:
    ///   - IMemoryCache → Cache altyapısı
    ///   - IHttpContextAccessor → HTTP context'e erişim
    ///   - ICacheManager (Singleton) → MemoryCacheManager
    ///   - IUserContextService (Scoped) → JWT claim okuma
    /// </summary>
    public class CoreModule : ICoreModule
    {
        public void Load(IServiceCollection services)
        {
            // MemoryCache altyapısı
            services.AddMemoryCache();

            // HTTP Context'e tüm katmanlardan erişim
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Cache yöneticisi - Singleton (thread-safe, state'ler paylaşılır)
            services.AddSingleton<ICacheManager, MemoryCacheManager>();

            // Kullanıcı context servisi - Scoped (her request yeni instance)
            services.AddScoped<IUserContextService, UserContextService>();
        }
    }
}
