using Entities.Dtos.Product;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation
{
    /// <summary>
    /// ProductAddDto ve ProductUpdateDto doğrulamalarını yapan FluentValidation sınıfı.
    /// ValidationAspect(typeof(ProductValidator)) ile entegre çalışır.
    /// </summary>
    public class ProductValidator : AbstractValidator<ProductAddDto>
    {
        public ProductValidator()
        {
            RuleFor(p => p.Name)
                .NotEmpty().WithMessage("Ürün adı boş geçilemez.")
                .MinimumLength(2).WithMessage("Ürün adı en az 2 karakter olmalıdır.")
                .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olmalıdır.");

            RuleFor(p => p.Price)
                .GreaterThan(0).WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.");

            RuleFor(p => p.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("Ürün stoğu negatif olamaz.");

            RuleFor(p => p.CategoryId)
                .GreaterThan(0).WithMessage("Geçerli bir kategori belirtilmelidir.");
        }
    }
}
