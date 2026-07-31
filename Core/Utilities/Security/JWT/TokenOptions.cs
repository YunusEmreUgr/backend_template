namespace Core.Utilities.Security.JWT
{
    /// <summary>
    /// JWT token üretimi için gerekli konfigürasyon.
    /// appsettings.json'daki "TokenOptions" bölümüne karşılık gelir.
    /// 
    /// appsettings.json örneği:
    /// "TokenOptions": {
    ///   "Audience": "myapp.api",
    ///   "Issuer": "myapp.api",
    ///   "AccessTokenExpiration": 60,
    ///   "SecurityKey": "your-super-secret-key-minimum-32-chars!!"
    /// }
    /// 
    /// AccessTokenExpiration → dakika cinsinden (60 = 1 saat)
    /// SecurityKey → en az 32 karakter olmalıdır (256-bit)
    /// </summary>
    public class TokenOptions
    {
        public string Audience { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public int AccessTokenExpiration { get; set; }
        public string SecurityKey { get; set; } = null!;
    }
}
