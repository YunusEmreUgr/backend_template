using FluentValidation;

namespace Core.CrossCuttingConcerns.Validation
{
    /// <summary>
    /// FluentValidation entegrasyon aracı.
    /// ValidationAspect tarafından çağrılır.
    /// 
    /// Örnek:
    ///   ValidationTool.Validate(new ProductAddValidator(), productAddDto);
    ///   // Hata varsa FluentValidation.ValidationException fırlatır.
    ///   // ExceptionMiddleware bunu yakalar ve 400 Bad Request döner.
    /// </summary>
    public class ValidationTool
    {
        /// <summary>
        /// Verilen validator'ı çalıştırır.
        /// Validasyon başarısız olursa ValidationException fırlatır.
        /// </summary>
        /// <typeparam name="T">Validate edilecek nesne tipi</typeparam>
        /// <param name="validator">FluentValidation validator instance'ı</param>
        /// <param name="entity">Validate edilecek nesne</param>
        public static void Validate<T>(AbstractValidator<T> validator, T entity)
        {
            var context = new ValidationContext<T>(entity);
            var result = validator.Validate(context);

            if (!result.IsValid)
            {
                // ValidationException fırlatır → ExceptionMiddleware yakalar → 400 Bad Request
                throw new ValidationException(result.Errors);
            }
        }
    }
}
