using Core.Entities.Abstract;

namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Apple kullanıcı bilgileri DTO
    /// </summary>
    public class AppleUserInfoDto : IDto
    {
        public string AppleId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool EmailVerified { get; set; } = true;
    }
}
