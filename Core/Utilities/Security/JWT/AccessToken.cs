namespace Core.Utilities.Security.JWT
{
    /// <summary>
    /// JWT erişim token'ı modeli.
    /// Bu nesne başarılı giriş/yenileme sonucunda client'a döndürülür.
    /// Client bu token'ı Authorization: Bearer {Token} header'ında kullanır.
    /// </summary>
    public class AccessToken
    {
        /// <summary>JWT token string'i (Base64 encoded)</summary>
        public string Token { get; set; } = null!;

        /// <summary>Token'ın UTC sona erme tarihi</summary>
        public DateTime Expiration { get; set; }
    }
}
