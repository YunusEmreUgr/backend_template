namespace Core.Utilities.Exceptions
{
    /// <summary>
    /// Uygulamada kullanılan hata kodlarının merkezi sabit listesi.
    /// Bu kodlar machine-readable olup API yanıtlarında "errorCode" alanında dönülür.
    /// Frontend bu kodlara göre kullanıcıya özel mesaj/aksiyon gösterebilir.
    /// 
    /// Yeni hata tipi eklerken burada bir sabit tanımlayın ve ExceptionMiddleware'de handle edin.
    /// </summary>
    public static class ErrorCodes
    {
        // ─── Doğrulama Hataları ────────────────────────────────────────────────
        /// <summary>FluentValidation gibi doğrulama başarısız olduğunda</summary>
        public const string ValidationError = "validation_error";

        // ─── Yetkilendirme Hataları ────────────────────────────────────────────
        /// <summary>İşlemi yapmaya yetkisi olmayan kullanıcı (403 Forbidden)</summary>
        public const string AccessDenied = "access_denied";

        /// <summary>Token geçersiz, süresi dolmuş veya oturum bulunamadı (401)</summary>
        public const string AuthenticationFailed = "authentication_failed";

        // ─── Kaynak Hataları ──────────────────────────────────────────────────
        /// <summary>İstenen kayıt bulunamadı (404 Not Found)</summary>
        public const string ResourceNotFound = "resource_not_found";

        /// <summary>Kaydın zaten mevcut olduğu durumlarda (409 Conflict)</summary>
        public const string Conflict = "conflict_error";

        // ─── Rate Limiting ────────────────────────────────────────────────────
        /// <summary>Rate limit aşıldığında (429 Too Many Requests)</summary>
        public const string TooManyRequests = "too_many_requests";

        // ─── Genel Hatalar ────────────────────────────────────────────────────
        /// <summary>Genel iş mantığı hatası (400 Bad Request)</summary>
        public const string GeneralFailure = "general_failure";

        /// <summary>Beklenmeyen sistem hatası (500 Internal Server Error)</summary>
        public const string UnexpectedError = "unexpected_error";
    }
}
