using Business.Abstract;
using Business.Constants;
using Core.Entities.Concrete.Users;
using Core.Utilities.Exceptions;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.JWT;
using Entities.Dtos.Auth;
using Microsoft.AspNetCore.Http;
using Core.Aspects.Autofac.Validation;
using Business.ValidationRules.FluentValidation;
using static Core.Utilities.Security.JWT.JwtHelper;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Concrete
{
    /// <summary>
    /// Kimlik doğrulama ve yetkilendirme iş mantığını yöneten somut servis sınıfı.
    /// Kayıt, giriş, şifre doğrulama ve token (JWT) üretim süreçlerini koordine eder.
    /// </summary>
    public class AuthManager : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ITokenHelper _tokenHelper;
        private readonly IUserOperationClaimService _userOperationClaimService;

        public AuthManager(IUserService userService, ITokenHelper tokenHelper, IUserOperationClaimService userOperationClaimService)
        {
            _userService = userService;
            _tokenHelper = tokenHelper;
            _userOperationClaimService = userOperationClaimService;
        }

        /// <inheritdoc/>
        [ValidationAspect(typeof(RegisterValidator))]
        public async Task<IDataResult<User>> Register(UserForRegisterDto userForRegisterDto, string password)
        {
            // Şifreyi HMACSHA512 kullanarak hash'le ve salt değerini oluştur
            HashingHelper.CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User
            {
                Email = userForRegisterDto.Email,
                FirstName = userForRegisterDto.FirstName,
                LastName = userForRegisterDto.LastName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Status = true // Varsayılan olarak aktif
            };

            // Kullanıcıyı veritabanına ekle
            await _userService.Add(user);

            // Varsayılan yetkiyi ata ("User" rolü)
            await _userOperationClaimService.AddUserClaim(user.UserId);

            return new SuccessDataResult<User>(user, Messages.UserRegistered, StatusCodes.Status201Created);
        }

        /// <inheritdoc/>
        [ValidationAspect(typeof(LoginValidator))]
        public async Task<IDataResult<User>> Login(UserForLoginDto userForLoginDto)
        {
            // E-posta adresine göre kullanıcıyı ara
            var userToCheck = await _userService.GetByMail(userForLoginDto.Email);
            if (userToCheck == null)
            {
                return new ErrorDataResult<User>(Messages.UserNotFound, StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound);
            }

            // Sosyal giriş yapmış kullanıcılar şifreyle giriş yapamaz (PasswordHash null'dır)
            if (userToCheck.PasswordHash == null || userToCheck.PasswordSalt == null)
            {
                return new ErrorDataResult<User>(
                    "Bu hesap sosyal medya hesabı (Google/Apple) ile ilişkilendirilmiştir. Lütfen sosyal giriş yöntemini kullanın.", 
                    StatusCodes.Status400BadRequest, 
                    ErrorCodes.ValidationError);
            }

            // Şifreyi tuz (salt) bilgisiyle birlikte hash'leyerek doğrula
            if (!HashingHelper.VerifyPasswordHash(userForLoginDto.Password, userToCheck.PasswordHash, userToCheck.PasswordSalt))
            {
                return new ErrorDataResult<User>(Messages.PasswordError, StatusCodes.Status401Unauthorized, ErrorCodes.AuthenticationFailed);
            }

            // Kullanıcının sistem durumu kontrolü (Ban durumu)
            if (!userToCheck.Status)
            {
                return new ErrorDataResult<User>(Messages.UserBanned, StatusCodes.Status403Forbidden, ErrorCodes.AccessDenied);
            }

            return new SuccessDataResult<User>(userToCheck, Messages.SuccessfulLogin, StatusCodes.Status200OK);
        }

        /// <inheritdoc/>
        public async Task<IResult> UserExists(string email)
        {
            var user = await _userService.GetByMail(email);
            if (user != null)
            {
                return new ErrorResult(Messages.UserAlreadyExists, StatusCodes.Status409Conflict, ErrorCodes.Conflict);
            }
            return new SuccessResult(StatusCodes.Status200OK);
        }

        /// <inheritdoc/>
        public IDataResult<AccessToken> CreateAccessToken(User user)
        {
            var claims = _userService.GetClaims(user);
            var accessToken = _tokenHelper.CreateToken(user, claims);
            return new SuccessDataResult<AccessToken>(accessToken, Messages.AccessTokenCreated, StatusCodes.Status200OK);
        }

        /// <inheritdoc/>
        public async Task<TokenDto> CreateAccessAndRefreshTokenAsync(User user, string ipAddress)
        {
            var claims = _userService.GetClaims(user);
            return await _tokenHelper.CreateTokensAsync(user, claims, ipAddress);
        }

        /// <inheritdoc/>
        public IDataResult<List<OperationClaim>> GetClaims(User user)
        {
            var claims = _userService.GetClaims(user);
            return new SuccessDataResult<List<OperationClaim>>(claims, StatusCodes.Status200OK);
        }
    }
}
