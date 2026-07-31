using Core.Entities.Abstract;

namespace Core.Entities.Concrete.Users
{
    /// <summary>
    /// Sistemdeki kullanıcıyı temsil eden entity sınıfı.
    /// 
    /// Özellikler:
    /// - Normal email/şifre ile kayıt desteklenir (PasswordHash + PasswordSalt)
    /// - Google OAuth ile kayıt desteklenir (GoogleId) - şifre olmadan
    /// - Email doğrulama akışı desteklenir (EmailConfirmationToken)
    /// - Şifre sıfırlama akışı desteklenir (PasswordResetToken)
    /// - Kullanıcı ban kontrolü için Status alanı (false = banlı)
    /// - Soft delete için IsDeleted + global query filter
    /// - Timestamp'ler için CreatedAt / UpdatedAt / LastLoginAt
    /// </summary>
    public class User : IAuditableEntity
    {
        /// <summary>Birincil anahtar (PK)</summary>
        public int UserId { get; set; }

        // ─── Temel Bilgiler ───────────────────────────────────────────────────
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        /// <summary>Benzersiz email adresi - kullanıcı adı olarak da kullanılır</summary>
        public string Email { get; set; } = null!;

        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }

        // ─── Kimlik Doğrulama (Email/Şifre) ──────────────────────────────────
        /// <summary>
        /// HMACSHA512 ile hashlenmiş şifre.
        /// Google OAuth kullanıcıları için null olabilir.
        /// </summary>
        public byte[]? PasswordHash { get; set; }

        /// <summary>HMACSHA512 salt değeri - şifre hash'ini doğrulamak için gerekli</summary>
        public byte[]? PasswordSalt { get; set; }

        // ─── Email Doğrulama ─────────────────────────────────────────────────
        public bool EmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiry { get; set; }

        // ─── Şifre Sıfırlama ─────────────────────────────────────────────────
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // ─── Sosyal Giriş ────────────────────────────────────────────────────
        /// <summary>Google OAuth ile giriş yapan kullanıcıların Google ID'si</summary>
        public string? GoogleId { get; set; }

        /// <summary>Apple Sign In ile giriş yapan kullanıcıların Apple ID'si</summary>
        public string? AppleId { get; set; }

        // ─── Durum ───────────────────────────────────────────────────────────
        /// <summary>
        /// true = aktif, false = banlı.
        /// UserStatusMiddleware her request'te bu değeri cache'li olarak kontrol eder.
        /// </summary>
        public bool Status { get; set; } = true;

        /// <summary>Soft delete bayrağı. HasQueryFilter ile tüm sorgulardan otomatik filtrelenir.</summary>
        public bool IsDeleted { get; set; } = false;

        // ─── Timestamp'ler ────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
