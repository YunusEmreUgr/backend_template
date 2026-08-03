using Business.Abstract;
using Entities.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Asp.Versioning;

namespace WebApi.Controllers
{
    /// <summary>
    /// Kimlik doğrulama işlemlerini (Giriş, Kayıt, Token Yenileme) yöneten Controller sınıfı.
    /// Güvenlik gereği RefreshToken'ları HTTP-Only Cookie olarak yönetir.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IAppleAuthService _appleAuthService;

        public AuthController(
            IAuthService authService,
            IGoogleAuthService googleAuthService,
            IAppleAuthService appleAuthService)
        {
            _authService = authService;
            _googleAuthService = googleAuthService;
            _appleAuthService = appleAuthService;
        }

        /// <summary>Yeni kullanıcı kaydı oluşturur.</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserForRegisterDto userForRegisterDto)
        {
            // Kullanıcı önceden kayıt olmuş mu?
            var userExists = await _authService.UserExists(userForRegisterDto.Email);
            if (!userExists.Success)
            {
                return StatusCode(userExists.StatusCode, userExists);
            }

            // Kaydı tamamla
            var registerResult = await _authService.Register(userForRegisterDto, userForRegisterDto.Password);
            if (!registerResult.Success)
            {
                return StatusCode(registerResult.StatusCode, registerResult);
            }

            // Kayıt sonrası doğrudan Access ve Refresh token üret
            var ipAddress = GetIpAddress();
            var tokenResult = await _authService.CreateAccessAndRefreshTokenAsync(registerResult.Data, ipAddress);

            // Refresh token'ı güvenli HTTP-Only Cookie olarak kaydet
            SetRefreshTokenCookie(tokenResult.RefreshToken, tokenResult.RefreshTokenExpiration);

            return Created(string.Empty, new
            {
                success = true,
                message = registerResult.Message,
                data = new
                {
                    token = tokenResult.AccessToken.Token,
                    expiration = tokenResult.AccessToken.Expiration,
                    refreshToken = tokenResult.RefreshToken,
                    refreshTokenExpiration = tokenResult.RefreshTokenExpiration
                }
            });
        }

        /// <summary>Kullanıcı girişi yapar ve JWT token'ları döner.</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserForLoginDto userForLoginDto)
        {
            var loginResult = await _authService.Login(userForLoginDto);
            if (!loginResult.Success)
            {
                return StatusCode(loginResult.StatusCode, loginResult);
            }

            // Giriş başarılı → Token'ları oluştur
            var ipAddress = GetIpAddress();
            var tokenResult = await _authService.CreateAccessAndRefreshTokenAsync(loginResult.Data, ipAddress);

            // Refresh token'ı Cookie olarak ayarla (güvenlik optimizasyonu)
            SetRefreshTokenCookie(tokenResult.RefreshToken, tokenResult.RefreshTokenExpiration);

            return Ok(new
            {
                success = true,
                message = loginResult.Message,
                data = new
                {
                    token = tokenResult.AccessToken.Token,
                    expiration = tokenResult.AccessToken.Expiration,
                    refreshToken = tokenResult.RefreshToken,
                    refreshTokenExpiration = tokenResult.RefreshTokenExpiration
                }
            });
        }

        /// <summary>Google OAuth ile giriş yapar veya otomatik kayıt oluşturur.</summary>
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthDto googleAuthDto)
        {
            var verifyResult = await _googleAuthService.VerifyAndGetGoogleUserInfoAsync(googleAuthDto.IdToken);
            if (!verifyResult.Success)
            {
                return BadRequest(verifyResult);
            }

            var loginResult = await _googleAuthService.GoogleLoginOrRegisterAsync(
                verifyResult.Data, 
                googleAuthDto.FirstName, 
                googleAuthDto.LastName);

            if (!loginResult.Success)
            {
                return BadRequest(loginResult);
            }

            var ipAddress = GetIpAddress();
            var tokenResult = await _authService.CreateAccessAndRefreshTokenAsync(loginResult.Data, ipAddress);

            SetRefreshTokenCookie(tokenResult.RefreshToken, tokenResult.RefreshTokenExpiration);

            return Ok(new
            {
                success = true,
                message = loginResult.Message,
                data = tokenResult.AccessToken
            });
        }

        /// <summary>Apple Sign-In ile giriş yapar veya otomatik kayıt oluşturur.</summary>
        [HttpPost("apple-login")]
        public async Task<IActionResult> AppleLogin([FromBody] AppleAuthDto appleAuthDto)
        {
            var verifyResult = await _appleAuthService.VerifyAndGetAppleUserInfoAsync(appleAuthDto);
            if (!verifyResult.Success)
            {
                return BadRequest(verifyResult);
            }

            var loginResult = await _appleAuthService.AppleLoginOrRegisterAsync(verifyResult.Data);
            if (!loginResult.Success)
            {
                return BadRequest(loginResult);
            }

            var ipAddress = GetIpAddress();
            var tokenResult = await _authService.CreateAccessAndRefreshTokenAsync(loginResult.Data, ipAddress);

            SetRefreshTokenCookie(tokenResult.RefreshToken, tokenResult.RefreshTokenExpiration);

            return Ok(new
            {
                success = true,
                message = loginResult.Message,
                data = tokenResult.AccessToken
            });
        }

        /// <summary>Giriş yapmış kullanıcının yetki/rol listesini döner.</summary>
        [HttpGet("claims")]
        [Authorize]
        public async Task<IActionResult> GetUserClaims()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == "userid")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                var user = await _authService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    var claimsResult = await _authService.GetClaimsAsync(user);
                    if (claimsResult.Success)
                    {
                        var claimNames = claimsResult.Data.Select(c => c.OperationClaimName).ToList();
                        return Ok(new { success = true, data = claimNames });
                    }
                }
            }

            return Ok(new { success = true, data = new List<string>() });
        }

        /// <summary>Refresh token kullanarak yeni erişim token'ı alır.</summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto? dto)
        {
            var refreshToken = dto?.RefreshToken ?? Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return BadRequest(new { success = false, message = "Refresh token gereklidir." });
            }

            var ipAddress = GetIpAddress();
            var refreshResult = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
            if (!refreshResult.Success)
            {
                return StatusCode(refreshResult.StatusCode, refreshResult);
            }

            SetRefreshTokenCookie(refreshResult.Data.RefreshToken, refreshResult.Data.RefreshTokenExpiration);
            return Ok(new
            {
                success = true,
                message = refreshResult.Message,
                data = refreshResult.Data
            });
        }

        // ─── Yardımcı Metodlar ───────────────────────────────────────────────

        /// <summary>Refresh token'ı XSS saldırılarından korumak için HTTP-Only Cookie olarak set eder.</summary>
        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Javascript ile cookie okunamaz (XSS engeller)
                Expires = expires,
                Secure = true, // Sadece HTTPS üzerinden iletilir
                SameSite = SameSiteMode.Strict // CSRF saldırılarını önler
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        /// <summary>İsteği atan istemcinin IP adresini bulur.</summary>
        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"]!;
            
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        }
    }
}
