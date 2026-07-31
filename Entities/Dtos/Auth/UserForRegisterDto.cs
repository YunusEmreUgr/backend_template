using Core.Entities.Abstract;

namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Kullanıcı kaydolma isteği için DTO.
    /// Servis katmanında: RegisterValidator ile doğrulanır.
    /// </summary>
    public class UserForRegisterDto : IDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        /// <summary>Benzersiz email adresi (kullanıcı adı olarak kullanılır)</summary>
        public string Email { get; set; } = null!;

        /// <summary>En az 6 karakter, büyük/küçük harf ve rakam içermeli (validator ile kontrol)</summary>
        public string Password { get; set; } = null!;
    }
}
