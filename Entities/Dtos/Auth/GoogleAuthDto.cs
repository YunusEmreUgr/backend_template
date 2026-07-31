using Core.Entities.Abstract;

namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Google OAuth kimlik doğrulama DTO
    /// </summary>
    public class GoogleAuthDto : IDto
    {
        /// <summary>
        /// Google tarafından sağlanan ID Token
        /// </summary>
        public string IdToken { get; set; } = null!;

        /// <summary>
        /// Google tarafından sağlanan Access Token (isteğe bağlı)
        /// </summary>
        public string? AccessToken { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
