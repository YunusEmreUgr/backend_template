using Core.Entities.Abstract;

namespace Core.Entities.Concrete.Users
{
    /// <summary>
    /// JWT Refresh Token entity'si.
    /// 
    /// Güvenlik özellikleri:
    /// - Token plaintext olarak saklanmaz, SHA256 hash'i saklanır (TokenHash)
    /// - Token revoke edilebilir (Revoked alanı doldurulur)
    /// - Kullanılan token'ın hangi yeni token ile değiştirildiği takip edilir (ReplacedByToken)
    /// - Her token hangi IP'den oluşturulduğu bilinir (CreatedByIp)
    /// 
    /// Refresh Token Rotation: 
    ///   Her kullanımda eski token revoke edilir, yeni token oluşturulur.
    ///   Bu sayede token çalınsa bile kısa sürede geçersiz olur.
    /// </summary>
    public class RefreshToken : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        /// <summary>
        /// Token'ın SHA256 hash'i.
        /// Veritabanında plaintext token saklanmaz.
        /// </summary>
        public string TokenHash { get; set; } = null!;

        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
        public string CreatedByIp { get; set; } = null!;

        // ─── Revoke Bilgileri ─────────────────────────────────────────────────
        /// <summary>Bu token kullanıldığında yerine geçen yeni token'ın hash'i</summary>
        public string? ReplacedByToken { get; set; }

        /// <summary>Token revoke edildiğinde UTC timestamp</summary>
        public DateTime? Revoked { get; set; }

        public string? RevokedByIp { get; set; }
        public string? ReasonRevoked { get; set; }

        // ─── Hesaplanan Özellikler ───────────────────────────────────────────
        /// <summary>Token aktif mi? (Revoke edilmemiş VE süresi dolmamış)</summary>
        public bool IsActive => Revoked == null && !IsExpired;

        /// <summary>Token'ın süresi dolmuş mu?</summary>
        public bool IsExpired => DateTime.UtcNow >= Expires;
    }
}
