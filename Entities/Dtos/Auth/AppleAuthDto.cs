using Core.Entities.Abstract;

namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Apple OAuth kimlik doğrulama DTO
    /// </summary>
    public class AppleAuthDto : IDto
    {
        public string IdentityToken { get; set; } = null!;
        public string? AuthorizationCode { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? Email { get; set; }
    }
}
