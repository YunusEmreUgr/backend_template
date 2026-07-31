using Core.Entities.Abstract;

namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Google kullanıcı bilgileri DTO
    /// </summary>
    public class GoogleUserInfoDto : IDto
    {
        /// <summary>
        /// Google kullanıcı ID (sub)
        /// </summary>
        public string GoogleId { get; set; } = null!;

        /// <summary>
        /// Kullanıcı email adresi
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Kullanıcı adı
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Profil resmi URL
        /// </summary>
        public string? Picture { get; set; }

        /// <summary>
        /// Email doğrulanmış mı
        /// </summary>
        public bool EmailVerified { get; set; } = true;
    }
}
