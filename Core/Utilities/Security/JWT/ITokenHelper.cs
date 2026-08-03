using Core.Entities.Concrete.Users;
using System.Security.Claims;

namespace Core.Utilities.Security.JWT
{
    /// <summary>
    /// JWT token oluşturma işlemleri için soyutlama interface'i.
    /// 
    /// Bu interface sayesinde farklı token stratejileri (JWT, OAuth, vb.)
    /// Business katmanından bağımsız olarak değiştirilebilir.
    /// 
    /// Implementasyon: JwtHelper
    /// Servis kayıt: builder.RegisterType&lt;JwtHelper&gt;().As&lt;ITokenHelper&gt;().SingleInstance();
    /// </summary>
    public interface ITokenHelper
    {
        /// <summary>
        /// Sadece AccessToken oluşturur (RefreshToken olmadan).
        /// Kullanım: Hızlı işlemler veya refresh gerektirmeyen senaryolar.
        /// </summary>
        AccessToken CreateToken(User user, List<OperationClaim> operationClaims);

        /// <summary>
        /// Hem AccessToken hem de RefreshToken oluşturur.
        /// Standart login/register akışında kullanılır.
        /// RefreshToken veritabanına kaydedilir.
        /// </summary>
        Task<JwtHelper.TokenDto> CreateTokensAsync(User user, List<OperationClaim> operationClaims, string ipAddress);

        /// <summary>
        /// Süresi dolmuş token'dan kullanıcının ClaimsPrincipal bilgisini ayıklar.
        /// </summary>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
