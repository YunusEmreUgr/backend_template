using Entities.Dtos.Auth;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation
{
    /// <summary>
    /// UserForRegisterDto için doğrulama kuralları.
    /// Kayıt işlemi öncesinde şifre gücü ve email formatı doğrulaması yapar.
    /// </summary>
    public class RegisterValidator : AbstractValidator<UserForRegisterDto>
    {
        public RegisterValidator()
        {
            RuleFor(u => u.FirstName)
                .NotEmpty().WithMessage("Ad alanı boş geçilemez.")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olmalıdır.");

            RuleFor(u => u.LastName)
                .NotEmpty().WithMessage("Soyad alanı boş geçilemez.")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olmalıdır.");

            RuleFor(u => u.Email)
                .NotEmpty().WithMessage("E-posta adresi boş geçilemez.")
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

            RuleFor(u => u.Password)
                .NotEmpty().WithMessage("Şifre boş geçilemez.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
                // Regex: en az bir büyük harf, bir küçük harf ve bir rakam içermeli
                .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
                .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
                .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermelidir.");
        }
    }
}
