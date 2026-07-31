namespace Core.Utilities.IoC
{
    /// <summary>
    /// Aspect'ler ve statik context'ler için Service Locator pattern.
    /// 
    /// KULLANIM DURUMU:
    /// Castle DynamicProxy intercept attribute'ları [Attribute] sınıflarıdır.
    /// Bu sınıflar constructor injection ile servis alamazlar.
    /// Bu nedenle ServiceTool.ServiceProvider.GetService&lt;T&gt;() kullanımı zorunludur.
    /// 
    /// UYARI:
    /// Normal kod akışında Constructor Injection her zaman tercih edilmelidir.
    /// Bu sadece Aspect'ler gibi injection'ın mümkün olmadığı durumlarda kullanılır.
    /// 
    /// KULLANIM:
    ///   // Program.cs - app build edildikten SONRA çağrılmalı
    ///   ServiceTool.Create(app.Services);
    /// 
    ///   // Aspect içinde:
    ///   var cache = ServiceTool.ServiceProvider.GetService&lt;ICacheManager&gt;();
    /// </summary>
    public static class ServiceTool
    {
        /// <summary>
        /// IServiceProvider thread-safe olduğu için static tutulabilir.
        /// app.Build() çağrısından sonra set edilir.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        /// <summary>
        /// Program.cs'te app build edildikten sonra çağrılır.
        /// Bu çağrı olmadan Aspect'ler çalışmaz.
        /// </summary>
        public static IServiceProvider Create(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            return serviceProvider;
        }
    }
}
