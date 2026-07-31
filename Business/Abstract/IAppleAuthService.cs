using Core.Entities.Concrete.Users;
using Core.Utilities.Results;
using Entities.Dtos.Auth;
using System.Threading.Tasks;

namespace Business.Abstract
{
    /// <summary>
    /// Apple OAuth kimlik doğrulama servisi
    /// </summary>
    public interface IAppleAuthService
    {
        /// <summary>
        /// Apple Identity Token'ı doğrula ve kullanıcı bilgilerini al
        /// </summary>
        Task<IDataResult<AppleUserInfoDto>> VerifyAndGetAppleUserInfoAsync(AppleAuthDto appleAuthDto);

        /// <summary>
        /// Apple kullanıcısı ile giriş yap veya kayıt ol
        /// </summary>
        Task<IDataResult<User>> AppleLoginOrRegisterAsync(AppleUserInfoDto appleUserInfo);
    }
}
