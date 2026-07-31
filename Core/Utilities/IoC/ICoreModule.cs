using Microsoft.Extensions.DependencyInjection;

namespace Core.Utilities.IoC
{
    /// <summary>
    /// Core katmanının modüler bağımlılık ekleme arayüzü.
    /// 
    /// Her "Core Module" bu interface'i implement eder ve
    /// ServiceCollectionExtensions.AddDependencyResolvers() ile yüklenir.
    /// 
    /// Bu pattern sayesinde Core katmanının bağımlılıkları merkezi ve 
    /// organize bir şekilde yönetilir.
    /// 
    /// Örnek:
    ///   public class CoreModule : ICoreModule
    ///   {
    ///       public void Load(IServiceCollection services)
    ///       {
    ///           services.AddMemoryCache();
    ///           services.AddSingleton&lt;ICacheManager, MemoryCacheManager&gt;();
    ///       }
    ///   }
    /// </summary>
    public interface ICoreModule
    {
        void Load(IServiceCollection serviceCollection);
    }
}
