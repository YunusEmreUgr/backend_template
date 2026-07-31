using System.Security.Claims;

namespace Core.Extensions
{
    /// <summary>
    /// ClaimsPrincipal (HttpContext.User) için extension metodları.
    /// SecuredOperation aspect'i ve servisler tarafından kullanılır.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Belirli tip claim'lerin değerlerini liste olarak döner.</summary>
        public static List<string> Claims(this ClaimsPrincipal claimsPrincipal, string claimType)
        {
            return claimsPrincipal?.FindAll(claimType)?.Select(x => x.Value).ToList()
                   ?? new List<string>();
        }

        /// <summary>
        /// Kullanıcının sahip olduğu rollerin listesini döner.
        /// SecuredOperation aspect'i bunu kullanarak yetki kontrolü yapar.
        /// </summary>
        public static List<string> ClaimRoles(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal?.Claims(ClaimTypes.Role) ?? new List<string>();
        }
    }
}
