namespace Entities.Dtos.Auth
{
    /// <summary>
    /// Refresh token yenileme isteği DTO sınıfı.
    /// Mobile/Flutter ve Cookie desteklemeyen istemciler için kullanılır.
    /// </summary>
    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; } = null!;
    }
}
