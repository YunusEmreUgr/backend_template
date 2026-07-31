using Entities.Dtos.Auth;
using FluentValidation;

namespace Business.ValidationRules.FluentValidation
{
    /// <summary>
    /// UserForLoginDto için doğrulama kuralları.
    /// Giriş yaparken email ve şifre formatını doğrular.
    /// </summary>
    public class LoginValidator : AbstractValidator<UserForLoginDto>
    {
        public LoginValidator()
        {
            RuleFor(u => u.Email)
                .NotEmpty().WithMessage("E-posta adresi boş geçilemez.")
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

            RuleFor(u => u.Password)
                .NotEmpty().WithMessage("Şifre boş geçilemez.");
        }
    }
}
