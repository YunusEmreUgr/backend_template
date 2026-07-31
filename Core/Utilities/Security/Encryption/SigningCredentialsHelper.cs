using Microsoft.IdentityModel.Tokens;

namespace Core.Utilities.Security.Encryption
{
    /// <summary>
    /// JWT token için HMACSHA512 imzalama bilgisi oluşturur.
    /// HMACSHA512 → 512-bit hash → güçlü güvenlik.
    /// </summary>
    public class SigningCredentialsHelper
    {
        /// <summary>SymmetricSecurityKey ile HMACSHA512 SigningCredentials oluşturur.</summary>
        public static SigningCredentials CreateSigningCredentials(SecurityKey securityKey)
        {
            return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512Signature);
        }
    }
}
