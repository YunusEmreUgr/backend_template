using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Core.Utilities.Security.Encryption
{
    /// <summary>
    /// JWT imzalama için SymmetricSecurityKey oluşturur.
    /// appsettings.json'daki SecurityKey string'ini SymmetricSecurityKey'e dönüştürür.
    /// 
    /// GÜVENLIK: SecurityKey en az 256-bit (32 karakter) olmalıdır.
    /// Üretim ortamında key'i environment variable veya secret manager'dan okuyun.
    /// </summary>
    public class SecurityKeyHelper
    {
        /// <summary>
        /// String key'i SymmetricSecurityKey'e dönüştürür.
        /// </summary>
        /// <param name="securityKey">appsettings.json'dan gelen secret key string</param>
        public static SecurityKey CreateSecurityKey(string securityKey)
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
        }
    }
}
