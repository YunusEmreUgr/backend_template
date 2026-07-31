using Castle.DynamicProxy;

namespace Core.Utilities.Interceptors
{
    /// <summary>
    /// Tüm aspect attribute'larının miras aldığı temel sınıf.
    /// 
    /// Castle DynamicProxy'nin IInterceptor interface'ini implement eder.
    /// Bu sayede Autofac bu attribute'ları AOP (Aspect-Oriented Programming) 
    /// için intercept noktaları olarak kullanabilir.
    /// 
    /// Priority: Birden fazla aspect olduğunda hangi sırayla çalışacağını belirler.
    ///   Düşük Priority → Önce çalışır
    ///   Örnek: CacheAspect Priority=1, SecuredOperation Priority=0 → Önce SecuredOperation
    /// 
    /// Kullanım: [SecuredOperation("Admin")] → SecuredOperation : MethodInterception
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class MethodInterceptionBaseAttribute : Attribute, IInterceptor
    {
        /// <summary>Aspect'in çalışma önceliği (düşük → önce)</summary>
        public int Priority { get; set; }

        /// <summary>
        /// Intercept edilen method çağrıldığında Castle tarafından çağrılır.
        /// Alt sınıflar bu metodu override ederek davranış ekler.
        /// </summary>
        public virtual void Intercept(IInvocation invocation)
        {
        }
    }
}
