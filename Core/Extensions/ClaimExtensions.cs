using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Core.Extensions
{
    /// <summary>
    /// ICollection&lt;Claim&gt; için JWT claim ekleme extension metodları.
    /// JwtHelper.SetClaims() içinde kullanılır.
    /// 
    /// Extension pattern kullanılarak kod okunabilirliği artırılmıştır.
    /// </summary>
    public static class ClaimExtensions
    {
        /// <summary>Email claim'i ekler (JwtRegisteredClaimNames.Email)</summary>
        public static void AddEmail(this ICollection<Claim> claims, string email)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        }

        /// <summary>Kullanıcı adı claim'i ekler (ClaimTypes.Name)</summary>
        public static void AddName(this ICollection<Claim> claims, string name)
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        /// <summary>Kullanıcı ID claim'i ekler (ClaimTypes.NameIdentifier)</summary>
        public static void AddNameIdentifier(this ICollection<Claim> claims, string nameIdentifier)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        }

        /// <summary>Rol claim'leri ekler - her rol ayrı Claim olarak eklenir (ClaimTypes.Role)</summary>
        public static void AddRoles(this ICollection<Claim> claims, string[] roles)
        {
            roles.ToList().ForEach(role => claims.Add(new Claim(ClaimTypes.Role, role)));
        }
    }
}
