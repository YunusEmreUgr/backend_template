using Core.Entities.Abstract;

namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Kullanıcı giriş isteği için veri transfer nesnesi (DTO).
    /// WebApi katmanında kullanıcıdan alınan Email ve Şifre bilgilerini taşır.
    /// </summary>
    public class UserForLoginDto : IDto
    {
        /// <summary>Kullanıcının kayıtlı e-posta adresi</summary>
        public string Email { get; set; } = null!;

        /// <summary>Kullanıcının şifresi</summary>
        public string Password { get; set; } = null!;
    }
}
