using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Core.Utilities.Security.UserContext
{
    /// <summary>
    /// IUserContextService'in IHttpContextAccessor tabanlı implementasyonu.
    /// 
    /// JWT Authentication middleware tarafından doğrulanan token'daki
    /// claim'leri okur ve strongly-typed bilgi döner.
    /// 
    /// ÖNEMLI: Bu servis yalnızca kimlik doğrulanmış request'lerde kullanılabilir.
    /// Anonim endpoint'lerde çağrılırsa hata fırlatır.
    /// </summary>
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public int GetUserId()
        {
            // JWT'de "userid" claim'i direkt ayarlandı, yoksa NameIdentifier'a fallback
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("userid")
                        ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null || !int.TryParse(claim.Value, out int userId))
                throw new UnauthorizedAccessException("Kullanıcı kimliği token'da bulunamadı.");

            return userId;
        }

        /// <inheritdoc/>
        public string GetUserEmail()
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)
                        ?? _httpContextAccessor.HttpContext?.User.FindFirst("email");

            return claim?.Value ?? throw new UnauthorizedAccessException("Email claim bulunamadı.");
        }

        /// <inheritdoc/>
        public string GetUserName()
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name);
            return claim?.Value ?? string.Empty;
        }
    }
}
