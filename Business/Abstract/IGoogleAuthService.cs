using Core.Utilities.Results;
using Entities.Dtos.Auth;

namespace Business.Abstract
{
    /// <summary>
    /// Google OAuth kimlik doğrulama servisi
    /// </summary>
    public interface IGoogleAuthService
    {
        /// <summary>
        /// Google ID Token'ı doğrula ve kullanıcı bilgilerini al
        /// </summary>
        /// <param name="idToken">Google ID Token</param>
        /// <returns>Doğrulanmış Google kullanıcı bilgileri</returns>
        Task<IDataResult<GoogleUserInfoDto>> VerifyAndGetGoogleUserInfoAsync(string idToken);

        /// <summary>
        /// Google kullanıcısı ile giriş yap veya kayıt ol
        /// </summary>
        /// <param name="googleUserInfo">Google kullanıcı bilgileri</param>
        /// <returns>Giriş sonucu ve kullanıcı bilgileri</returns>
        Task<IDataResult<Core.Entities.Concrete.Users.User>> GoogleLoginOrRegisterAsync(
            GoogleUserInfoDto googleUserInfo, 
            string? firstName = null,
            string? lastName = null);
    }
}
