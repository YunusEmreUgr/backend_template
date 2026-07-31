using Autofac;
using Autofac.Extras.DynamicProxy;
using Business.Abstract;
using Business.BusinessRules;
using Business.Concrete;
using Castle.DynamicProxy;
using Core.Utilities.Interceptors;
using Core.DataAccess;
using Core.Utilities.Security.JWT;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;

namespace Business.DependencyResolvers.Autofac
{
    /// <summary>
    /// Autofac Dependency Injection (DI) modülü.
    /// İş katmanı ve veri erişim katmanı servislerini sisteme kaydeder.
    /// AOP Aspect'lerinin (Yetkilendirme, Cache, Validasyon) metodlar üzerinde 
    /// çalışabilmesi için AOP proxy motorunu devreye sokar.
    /// </summary>
    public class AutofacBusinessModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // ─── Veri Erişim (DAL) Kayıtları ──────────────────────────────────────
            builder.RegisterType<EfProductDal>().As<IProductDal>().InstancePerLifetimeScope();
            builder.RegisterType<EfUserDal>().As<IUserDal>().InstancePerLifetimeScope();
            builder.RegisterType<EfOperationClaimDal>().As<IOperationClaimDal>().InstancePerLifetimeScope();
            builder.RegisterType<EfUserOperationClaimDal>().As<IUserOperationClaimDal>().InstancePerLifetimeScope();
            builder.RegisterType<RefreshTokenRepository>().As<IRefreshTokenRepository>().InstancePerLifetimeScope();

            // ─── İş Kuralları (Business Rules) Kayıtları ─────────────────────────
            builder.RegisterType<ProductBusinessRules>().InstancePerLifetimeScope();

            // ─── Güvenlik ve Yardımcı Kayıtlar ──────────────────────────────────
            builder.RegisterType<JwtHelper>().As<ITokenHelper>().SingleInstance();
            builder.RegisterType<GoogleAuthService>().As<IGoogleAuthService>().InstancePerLifetimeScope();
            builder.RegisterType<AppleAuthService>().As<IAppleAuthService>().InstancePerLifetimeScope();

            // ─── Servis (Manager) Kayıtları ve AOP Proxying ──────────────────────
            // Aşağıdaki kod "Manager" ile biten tüm somut sınıfları otomatik bulur, 
            // arayüzleriyle eşleştirir ve üzerlerindeki Aspect'leri etkinleştirir.
            
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            builder.RegisterAssemblyTypes(assembly)
                .Where(t => t.Name.EndsWith("Manager"))
                .AsImplementedInterfaces()
                .EnableInterfaceInterceptors(new ProxyGenerationOptions
                {
                    // Hangi aspect'lerin hangi sırayla çağrılacağını belirleyen seçici
                    Selector = new AspectInterceptorSelector()
                })
                .InstancePerLifetimeScope();
        }
    }
}
