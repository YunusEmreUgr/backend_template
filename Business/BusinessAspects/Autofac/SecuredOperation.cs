using Business.Constants;
using Castle.DynamicProxy;
using Core.Extensions;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Business.BusinessAspects.Autofac
{
    /// <summary>
    /// Metod bazlı rol yetkilendirmesi yapan AOP Aspect sınıfı.
    /// Metod çalışmadan önce kullanıcının belirtilen rollerden en az birine sahip olup olmadığını kontrol eder.
    /// 
    /// Kullanım:
    ///   [SecuredOperation("Admin,Moderator")]
    ///   public async Task<IResult> Add(Product product) { ... }
    /// </summary>
    public class SecuredOperation : MethodInterception
    {
        private readonly string[] _roles;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecuredOperation(string roles)
        {
            // Roller virgülle ayrılmış string olarak alınır, temizlenir ve standartlaştırılır
            _roles = roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .Select(r => r.Equals("admin", StringComparison.OrdinalIgnoreCase)
                    ? "Admin"
                    : r.Equals("moderator", StringComparison.OrdinalIgnoreCase)
                        ? "Moderator"
                        : r)
                .ToArray();

            // ServiceTool ile IHttpContextAccessor servisine statik olarak erişilir (Aspect constructor injection alamaz)
            _httpContextAccessor = ServiceTool.ServiceProvider.GetService<IHttpContextAccessor>()
                ?? throw new InvalidOperationException("IHttpContextAccessor servisine erişilemedi.");
        }

        /// <summary>
        /// Metod çalışmadan önce yetki kontrolü yapar. Yetki yoksa exception fırlatır.
        /// </summary>
        protected override void OnBefore(IInvocation invocation)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.User?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(Messages.AuthorizationDenied);
            }

            // Kullanıcının token içindeki rollerini al
            var roleClaims = httpContext.User.ClaimRoles();
            if (roleClaims != null && roleClaims.Count > 0)
            {
                roleClaims = roleClaims
                    .Select(r => r?.Trim())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Select(r => r!.Equals("admin", StringComparison.OrdinalIgnoreCase)
                        ? "Admin"
                        : r!.Equals("moderator", StringComparison.OrdinalIgnoreCase)
                            ? "Moderator"
                            : r!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // Kullanıcının rollerinden herhangi biri aspect rollerinde mevcut mu?
            foreach (var role in _roles)
            {
                if (roleClaims != null && roleClaims.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    return; // Yetki doğrulandı, metoda devam et
                }
            }

            // Yetkisiz erişim durumunda exception fırlatılır → ExceptionMiddleware 403 Forbidden döner
            throw new System.Security.SecurityException(Messages.AuthorizationDenied);
        }
    }
}
