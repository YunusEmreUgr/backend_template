namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Kullanıcı profil bilgileri DTO sınıfı.
    /// </summary>
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
