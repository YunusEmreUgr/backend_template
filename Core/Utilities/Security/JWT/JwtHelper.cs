using Core.DataAccess;
using Core.Entities.Concrete.Users;
using Core.Extensions;
using Core.Utilities.Security.Encryption;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Core.Utilities.Security.JWT
{
    /// <summary>
    /// ITokenHelper'ın JWT tabanlı implementasyonu.
    /// 
    /// AccessToken:
    ///   - HMACSHA512 ile imzalanmış JWT
    ///   - Claims: userId, email, name, roles, universityId (özelleştirilebilir)
    ///   - Expiration: TokenOptions.AccessTokenExpiration dakikası
    ///
    /// RefreshToken:
    ///   - 64 byte random değer → Base64 encoded
    ///   - SHA256 hash olarak veritabanında saklanır (güvenlik)
    ///   - 30 gün geçerli
    ///   - Token Rotation desteklenir
    /// 
    /// SingleInstance olarak kayıt edilir (thread-safe).
    /// </summary>
    public class JwtHelper : ITokenHelper
    {
        public IConfiguration Configuration { get; }
        private readonly TokenOptions _tokenOptions;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        // İç içe DTO sınıfı: Login response'unda kullanılır
        public class TokenDto
        {
            /// <summary>Kısa ömürlü JWT erişim token'ı</summary>
            public AccessToken AccessToken { get; set; } = null!;

            /// <summary>Uzun ömürlü yenileme token'ı (plaintext - sadece bir kez döner)</summary>
            public string RefreshToken { get; set; } = null!;

            /// <summary>RefreshToken'ın UTC sona erme tarihi</summary>
            public DateTime RefreshTokenExpiration { get; set; }
        }

        public JwtHelper(IConfiguration configuration, IRefreshTokenRepository refreshTokenRepository)
        {
            Configuration = configuration;
            _tokenOptions = Configuration.GetSection("TokenOptions").Get<TokenOptions>()
                ?? throw new InvalidOperationException("TokenOptions configuration is missing.");
            _refreshTokenRepository = refreshTokenRepository;
        }

        /// <inheritdoc/>
        public async Task<TokenDto> CreateTokensAsync(User user, List<OperationClaim> operationClaims, string ipAddress)
        {
            // AccessToken oluştur
            var accessToken = CreateToken(user, operationClaims);

            // RefreshToken oluştur (güvenli random)
            var refreshTokenPlain = GenerateRandomToken();

            // Veritabanına sadece hash'i sakla (güvenlik gereği)
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = HashToken(refreshTokenPlain),
                Expires = DateTime.UtcNow.AddDays(30),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            return new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenPlain,
                RefreshTokenExpiration = refreshTokenEntity.Expires
            };
        }

        /// <inheritdoc/>
        public AccessToken CreateToken(User user, List<OperationClaim> operationClaims)
        {
            var expiration = DateTime.Now.AddMinutes(_tokenOptions.AccessTokenExpiration);
            var securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey);
            var signingCredentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey);

            var jwt = new JwtSecurityToken(
                issuer: _tokenOptions.Issuer,
                audience: _tokenOptions.Audience,
                expires: expiration,
                notBefore: DateTime.Now,
                claims: SetClaims(user, operationClaims),
                signingCredentials: signingCredentials
            );

            return new AccessToken
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwt),
                Expiration = expiration
            };
        }

        // ─── Private Yardımcılar ─────────────────────────────────────────────

        /// <summary>
        /// JWT içine eklenecek claim'leri oluşturur.
        /// Uygulama ihtiyacına göre buraya yeni claim'ler eklenebilir.
        /// </summary>
        private IEnumerable<Claim> SetClaims(User user, List<OperationClaim> operationClaims)
        {
            var claims = new List<Claim>();

            // Standart JWT claim'leri
            claims.AddNameIdentifier(user.UserId.ToString());
            claims.AddEmail(user.Email ?? string.Empty);
            claims.AddName($"{user.FirstName} {user.LastName}");

            // Özel claim: userId (kolay erişim için)
            claims.Add(new Claim("userid", user.UserId.ToString()));

            // Roller (her rol ayrı Claim olarak eklenir)
            claims.AddRoles(operationClaims.Select(c => c.OperationClaimName).ToArray());

            // TODO: Uygulamaya özgü ek claim'ler buraya eklenebilir
            // Örnek: claims.Add(new Claim("tenantId", user.TenantId.ToString()));

            return claims;
        }

        /// <summary>
        /// Kriptografik olarak güvenli random token üretir.
        /// RefreshToken olarak kullanılır.
        /// </summary>
        private string GenerateRandomToken(int size = 64)
        {
            var bytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>Token'ı SHA256 ile hash'ler - veritabanında plaintext saklanmaz.</summary>
        private string HashToken(string token)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }

        /// <inheritdoc/>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims));
            }
            catch
            {
                return null;
            }
        }
    }
}
