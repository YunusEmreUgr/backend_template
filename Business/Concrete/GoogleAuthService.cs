using Business.Abstract;
using Core.Entities.Concrete.Users;
using Core.Utilities.Logging;
using Core.Utilities.Results;
using Entities.Dtos.Auth;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Business.Concrete
{
    /// <summary>
    /// Google OAuth kimlik doğrulama servisi somut sınıfı.
    /// Google ID Token'larının doğrulanması ve otomatik üyelik/giriş işlemlerini yönetir.
    /// </summary>
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IUserService _userService;
        private readonly ILoggerService _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        private const string GoogleTokenInfoUrl = "https://www.googleapis.com/oauth2/v3/tokeninfo";

        public GoogleAuthService(
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
        public async Task<IDataResult<GoogleUserInfoDto>> VerifyAndGetGoogleUserInfoAsync(string idToken)
        {
            try
            {
                _logger.LogInfo("Google ID Token doğrulanıyor");

                // 1. Token Format Kontrolü
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(idToken))
                {
                    _logger.LogWarning("Geçersiz Google token formatı");
                    return new ErrorDataResult<GoogleUserInfoDto>("Geçersiz token formatı");
                }

                var token = handler.ReadJwtToken(idToken);

                // 2. Süre Kontrolü
                if (token.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Google token süresi dolmuş");
                    return new ErrorDataResult<GoogleUserInfoDto>("Token süresi dolmuş");
                }

                // 3. Issuer (Yayıncı) Kontrolü
                var issuer = token.Issuer;
                if (!issuer.Contains("accounts.google.com"))
                {
                    _logger.LogWarning("Geçersiz Google issuer: {Issuer}", issuer);
                    return new ErrorDataResult<GoogleUserInfoDto>("Geçersiz token kaynağı");
                }

                // 4. Audience (Client ID) Kontrolü
                var aud = token.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;
                var configuredClientId = _configuration["SocialAuth:Google:ClientId"];
                var configuredIosClientId = _configuration["SocialAuth:Google:IosClientId"];

                if (!string.IsNullOrEmpty(configuredClientId) || !string.IsNullOrEmpty(configuredIosClientId))
                {
                    if (aud != configuredClientId && aud != configuredIosClientId)
                    {
                        _logger.LogWarning("Geçersiz Client ID. Alınan: {Received}", aud ?? "null");
                        return new ErrorDataResult<GoogleUserInfoDto>("Geçersiz Client ID (Uygulama uyuşmazlığı)");
                    }
                }
                else
                {
                    _logger.LogWarning("Google Client ID konfigürasyonu eksik. Güvenlik doğrulaması sınırlı!");
                }

                // 5. Google API Doğrulaması (Online Verification)
                try
                {
                    var response = await _httpClient.GetAsync($"{GoogleTokenInfoUrl}?id_token={idToken}");
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Google API online token doğrulama başarısız. Status: {Status}", response.StatusCode);
                        return new ErrorDataResult<GoogleUserInfoDto>("Token Google tarafından doğrulanamadı");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Google API erişim hatası (JWT doğrulama ile devam ediliyor): {Message}", ex.Message);
                }

                // 6. Bilgileri Çıkar
                var googleId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                var picture = token.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;
                var emailVerifiedClaim = token.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
                var emailVerified = emailVerifiedClaim != null && bool.Parse(emailVerifiedClaim);

                if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Google token içeriğinde kritik bilgiler (sub, email) eksik");
                    return new ErrorDataResult<GoogleUserInfoDto>("Token'da gerekli bilgiler eksik");
                }

                var googleUserInfo = new GoogleUserInfoDto
                {
                    GoogleId = googleId,
                    Email = email,
                    Name = name ?? email.Split('@')[0],
                    Picture = picture,
                    EmailVerified = emailVerified
                };

                return new SuccessDataResult<GoogleUserInfoDto>(googleUserInfo, "Token başarıyla doğrulandı");
            }
            catch (Exception ex)
            {
                _logger.LogError("Google token doğrulama hatası", ex);
                return new ErrorDataResult<GoogleUserInfoDto>($"Token doğrulama hatası: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IDataResult<User>> GoogleLoginOrRegisterAsync(
            GoogleUserInfoDto googleUserInfo,
            string? firstName = null,
            string? lastName = null)
        {
            try
            {
                _logger.LogInfo("Google kullanıcısı giriş/kayıt işlemi başlatılıyor. Email: {Email}", googleUserInfo.Email);

                // Önce GoogleId ile ara (en güvenli eşleştirme yöntemi)
                var existingUser = await _userService.GetByGoogleId(googleUserInfo.GoogleId);

                // GoogleId ile bulunamadıysa e-posta ile ara (manuel kayıtlı kullanıcı sosyal hesabı bağlamak isteyebilir)
                if (existingUser == null)
                {
                    existingUser = await _userService.GetByMail(googleUserInfo.Email);
                }

                if (existingUser != null)
                {
                    // --- MEVCUT KULLANICI ---
                    if (!existingUser.Status)
                    {
                        _logger.LogWarning("Banlı kullanıcı Google ile giriş yapmaya çalıştı. Email: {Email}", googleUserInfo.Email);
                        return new ErrorDataResult<User>("Bu hesap devre dışı bırakılmıştır");
                    }

                    bool needsUpdate = false;

                    // GoogleId eksikse bağla
                    if (string.IsNullOrEmpty(existingUser.GoogleId))
                    {
                        existingUser.GoogleId = googleUserInfo.GoogleId;
                        existingUser.EmailConfirmed = googleUserInfo.EmailVerified;
                        needsUpdate = true;
                    }

                    // İsim / Soyisim eksikse ve parametre olarak gönderilmişse güncelle
                    if (!string.IsNullOrEmpty(firstName) && (string.IsNullOrEmpty(existingUser.FirstName) || existingUser.FirstName == "User"))
                    {
                        existingUser.FirstName = firstName;
                        needsUpdate = true;
                    }
                    if (!string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(existingUser.LastName))
                    {
                        existingUser.LastName = lastName;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        await _userService.Update(existingUser);
                        _logger.LogInfo("Mevcut kullanıcının Google bilgileri güncellendi. Email: {Email}", googleUserInfo.Email);
                    }

                    return new SuccessDataResult<User>(existingUser, "Giriş başarılı");
                }

                // --- YENİ KULLANICI (OTOMATİK KAYIT) ---
                var newUser = new User
                {
                    Email = googleUserInfo.Email,
                    FirstName = !string.IsNullOrEmpty(firstName) ? firstName : ExtractFirstName(googleUserInfo.Name),
                    LastName = !string.IsNullOrEmpty(lastName) ? lastName : ExtractLastName(googleUserInfo.Name),
                    GoogleId = googleUserInfo.GoogleId,
                    EmailConfirmed = googleUserInfo.EmailVerified,
                    Status = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _userService.Add(newUser);
                _logger.LogInfo("Yeni Google kullanıcısı oluşturuldu. Email: {Email}", googleUserInfo.Email);

                return new SuccessDataResult<User>(newUser, "Kayıt başarılı");
            }
            catch (Exception ex)
            {
                _logger.LogError("Google giriş/kayıt işlemi sırasında veritabanı hatası", ex);
                return new ErrorDataResult<User>($"Giriş/Kayıt hatası: {ex.Message}");
            }
        }

        private string ExtractFirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "User";
            var parts = fullName.Trim().Split(' ');
            return parts[0];
        }

        private string ExtractLastName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Trim().Split(' ');
            if (parts.Length > 1)
            {
                return string.Join(" ", parts.Skip(1));
            }
            return "";
        }
    }
}
