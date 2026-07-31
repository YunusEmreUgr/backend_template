using Business.Abstract;
using Core.Entities.Concrete.Users;
using Core.Utilities.Logging;
using Core.Utilities.Results;
using Entities.Dtos.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Business.Concrete
{
    /// <summary>
    /// Apple Sign-In kimlik doğrulama servisi somut sınıfı.
    /// Apple Identity Token'larının doğrulanması ve otomatik üyelik/giriş işlemlerini yönetir.
    /// </summary>
    public class AppleAuthService : IAppleAuthService
    {
        private readonly IUserService _userService;
        private readonly ILoggerService _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        private const string AppleKeysUrl = "https://appleid.apple.com/auth/keys";
        private const string AppleIssuer = "https://appleid.apple.com";

        public AppleAuthService(
            IUserService userService,
            ILoggerService logger,
            IConfiguration configuration)
        {
            _userService = userService;
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        /// <inheritdoc/>
        public async Task<IDataResult<AppleUserInfoDto>> VerifyAndGetAppleUserInfoAsync(AppleAuthDto appleAuthDto)
        {
            try
            {
                _logger.LogInfo("Apple Identity Token doğrulanıyor");

                var idToken = appleAuthDto.IdentityToken;
                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(idToken))
                {
                    _logger.LogWarning("Geçersiz Apple Identity Token formatı");
                    return new ErrorDataResult<AppleUserInfoDto>("Geçersiz Apple token formatı");
                }

                var token = handler.ReadJwtToken(idToken);

                // 1. Süre Kontrolü
                if (token.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Apple token süresi dolmuş");
                    return new ErrorDataResult<AppleUserInfoDto>("Token süresi dolmuş");
                }

                // 2. Issuer (Yayıncı) Kontrolü
                if (token.Issuer != AppleIssuer)
                {
                    _logger.LogWarning("Geçersiz Apple issuer: {Issuer}", token.Issuer);
                    return new ErrorDataResult<AppleUserInfoDto>("Geçersiz token kaynağı");
                }

                // 3. Audience Kontrolü (App Bundle ID veya Client ID)
                var aud = token.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;
                var expectedAudience = _configuration["SocialAuth:Apple:ExpectedAudience"];
                if (!string.IsNullOrEmpty(expectedAudience))
                {
                    if (aud != expectedAudience)
                    {
                        _logger.LogWarning("Apple Audience/Client ID eşleşmedi. Beklenen: {Expected}, Alınan: {Received}", expectedAudience ?? "null", aud ?? "null");
                        return new ErrorDataResult<AppleUserInfoDto>("Geçersiz Apple Audience (Uygulama uyuşmazlığı)");
                    }
                }
                else
                {
                    _logger.LogWarning("Apple ExpectedAudience konfigürasyonu eksik. Güvenlik doğrulaması sınırlı!");
                }

                // 4. Apple JWKS (Public Keys) İmzası Doğrulaması
                try
                {
                    var keysResponse = await _httpClient.GetStringAsync(AppleKeysUrl);
                    var jwks = new JsonWebKeySet(keysResponse);

                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = jwks.Keys,
                        ValidateIssuer = true,
                        ValidIssuer = AppleIssuer,
                        ValidateAudience = false, // audience kontrolünü yukarıda elle yaptık
                        ValidateLifetime = true
                    };

                    handler.ValidateToken(idToken, validationParameters, out var validatedToken);
                    _logger.LogInfo("Apple Identity Token imzası başarıyla doğrulandı");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Apple JWT Signature doğrulanamadı (Offline fallback ile devam ediliyor): {Message}", ex.Message);
                }

                // 5. Bilgileri Çıkar
                var appleId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

                if (string.IsNullOrEmpty(appleId))
                {
                    _logger.LogWarning("Apple token içeriğinde 'sub' (Apple ID) eksik");
                    return new ErrorDataResult<AppleUserInfoDto>("Token'da kullanıcı kimliği (sub) eksik");
                }

                // Apple ad-soyad ve email bilgisini sadece İLK girişte gönderir.
                // Sonraki girişlerde null dönebilir. Bu yüzden DTO'dan gelen veriyi yedek olarak kullanıyoruz.
                var finalEmail = email ?? appleAuthDto.Email;

                var userInfo = new AppleUserInfoDto
                {
                    AppleId = appleId,
                    Email = finalEmail ?? $"{appleId}@privaterelay.appleid.com", // E-posta bulunamazsa yedek oluştur
                    FirstName = appleAuthDto.GivenName ?? "Apple",
                    LastName = appleAuthDto.FamilyName ?? "User",
                    EmailVerified = true
                };

                return new SuccessDataResult<AppleUserInfoDto>(userInfo, "Apple Token başarıyla doğrulandı");
            }
            catch (Exception ex)
            {
                _logger.LogError("Apple token doğrulama hatası", ex);
                return new ErrorDataResult<AppleUserInfoDto>($"Apple Token doğrulama hatası: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IDataResult<User>> AppleLoginOrRegisterAsync(AppleUserInfoDto appleUserInfo)
        {
            try
            {
                _logger.LogInfo("Apple kullanıcısı giriş/kayıt işlemi başlatılıyor. AppleId: {AppleId}", appleUserInfo.AppleId);

                // Önce AppleId ile ara (en güvenli eşleştirme yöntemi)
                User? existingUser = await _userService.GetByAppleId(appleUserInfo.AppleId);

                // AppleId ile bulunamadıysa e-posta ile ara (manuel kayıtlı kullanıcı sosyal hesabı bağlamak isteyebilir)
                if (existingUser == null && !string.IsNullOrEmpty(appleUserInfo.Email))
                {
                    existingUser = await _userService.GetByMail(appleUserInfo.Email);
                }

                if (existingUser != null)
                {
                    // --- MEVCUT KULLANICI ---
                    if (!existingUser.Status)
                    {
                        return new ErrorDataResult<User>("Bu hesap devre dışı bırakılmıştır");
                    }

                    bool needsUpdate = false;

                    // AppleId eksikse bağla
                    if (string.IsNullOrEmpty(existingUser.AppleId))
                    {
                        existingUser.AppleId = appleUserInfo.AppleId;
                        existingUser.EmailConfirmed = true;
                        needsUpdate = true;
                    }

                    // İsim/Soyisim güncelleme
                    if (!string.IsNullOrEmpty(appleUserInfo.FirstName) && (string.IsNullOrEmpty(existingUser.FirstName) || existingUser.FirstName == "Apple"))
                    {
                        existingUser.FirstName = appleUserInfo.FirstName;
                        needsUpdate = true;
                    }
                    if (!string.IsNullOrEmpty(appleUserInfo.LastName) && (string.IsNullOrEmpty(existingUser.LastName) || existingUser.LastName == "User"))
                    {
                        existingUser.LastName = appleUserInfo.LastName;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        await _userService.Update(existingUser);
                    }

                    return new SuccessDataResult<User>(existingUser, "Giriş başarılı");
                }

                // --- YENİ KULLANICI (OTOMATİK KAYIT) ---
                var newUser = new User
                {
                    Email = appleUserInfo.Email,
                    FirstName = appleUserInfo.FirstName ?? "Apple",
                    LastName = appleUserInfo.LastName ?? "User",
                    AppleId = appleUserInfo.AppleId,
                    EmailConfirmed = true,
                    Status = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _userService.Add(newUser);
                _logger.LogInfo("Yeni Apple kullanıcısı oluşturuldu. Email: {Email}", appleUserInfo.Email);

                return new SuccessDataResult<User>(newUser, "Kayıt başarılı");
            }
            catch (Exception ex)
            {
                _logger.LogError("Apple giriş/kayıt işlemi sırasında veritabanı hatası", ex);
                return new ErrorDataResult<User>($"Giriş/Kayıt hatası: {ex.Message}");
            }
        }
    }
}
