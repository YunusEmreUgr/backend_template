namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Şifre değiştirme isteği DTO sınıfı.
    /// </summary>
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
