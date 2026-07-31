using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Validation;
using Core.Utilities.Interceptors;
using FluentValidation;

namespace Core.Aspects.Autofac.Validation
{
    /// <summary>
    /// Metod çağrılmadan önce parametrelerini FluentValidation kurallarına göre doğrular.
    /// 
    /// Kullanım:
    ///   [ValidationAspect(typeof(ProductValidator))]
    ///   public async Task<IResult> Add(Product product) { ... }
    /// </summary>
    public class ValidationAspect : MethodInterception
    {
        private readonly Type _validatorType;

        public ValidationAspect(Type validatorType)
        {
            // validatorType'ın IValidator implement edip etmediğini kontrol et
            if (!typeof(IValidator).IsAssignableFrom(validatorType))
            {
                throw new ArgumentException("Verilen sınıf bir FluentValidation IValidator sınıfı değil.");
            }

            _validatorType = validatorType;
        }

        /// <summary>
        /// Metod çalışmadan hemen önce çağrılır (OnBefore).
        /// Parametreler arasından validator'ın doğrulayabileceği tipteki nesneleri seçer
        /// ve ValidationTool yardımıyla doğrular.
        /// </summary>
        protected override void OnBefore(IInvocation invocation)
        {
            // Validator'dan dinamik bir instance oluştur
            var validator = (IValidator)Activator.CreateInstance(_validatorType)!;

            // Validator'ın generic tipini al (örn: ProductValidator -> BaseType: AbstractValidator<Product> -> generic arg: Product)
            var entityType = _validatorType.BaseType?.GetGenericArguments()[0];
            if (entityType == null) return;

            // Metod argümanları arasından tipi uyuşan nesneleri bul
            var entities = invocation.Arguments.Where(t => t != null && t.GetType() == entityType);

            foreach (var entity in entities)
            {
                // ValidationTool.Validate generic olduğu için dynamic olarak çağırırız
                ValidationTool.Validate((dynamic)validator, (dynamic)entity);
            }
        }
    }
}
