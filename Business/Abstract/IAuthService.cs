using Core.Entities.Concrete.Users;
using Core.Utilities.Results;
using Core.Utilities.Security.JWT;
using Entities.Dtos.Auth;
using static Core.Utilities.Security.JWT.JwtHelper;

namespace Business.Abstract
{
    /// <summary>
    /// Kimlik doğrulama, kayıt ve token yönetimini sağlayan servis arayüzü.
    /// JWT authentication akışlarını yönetir.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Yeni kullanıcı kaydeder.</summary>
        Task<IDataResult<User>> Register(UserForRegisterDto userForRegisterDto, string password);

        /// <summary>Kullanıcı bilgilerini doğrular (şifre, durum ve ban durumu kontrolü).</summary>
        Task<IDataResult<User>> Login(UserForLoginDto userForLoginDto);

        /// <summary>E-posta adresiyle daha önce kayıt olunup olunmadığını kontrol eder.</summary>
        Task<IResult> UserExists(string email);

        /// <summary>Kullanıcı için tek başına Access Token üretir.</summary>
        IDataResult<AccessToken> CreateAccessToken(User user);

        /// <summary>Kullanıcı için hem Access Token hem de Refresh Token üretir ve veritabanına kaydeder.</summary>
        Task<TokenDto> CreateAccessAndRefreshTokenAsync(User user, string ipAddress);

        /// <summary>Kullanıcının sahip olduğu rolleri döner.</summary>
        IDataResult<List<OperationClaim>> GetClaims(User user);
    }
}
