using System.Text.Json.Serialization;

namespace Core.Utilities.Exceptions
{
    /// <summary>
    /// ExceptionMiddleware'in API yanıtı olarak döndürdüğü hata modeli.
    /// 
    /// JSON örneği:
    /// {
    ///   "success": false,
    ///   "traceId": "00-abc123-def456-00",
    ///   "timestamp": "2024-01-15T10:30:00Z",
    ///   "developerMessage": "System.NullReferenceException: ...",  // Sadece Development
    ///   "error": {
    ///     "code": "validation_error",
    ///     "message": "Lütfen formdaki hatalı alanları düzeltin.",
    ///     "details": {
    ///       "Email": ["Geçerli bir email giriniz."],
    ///       "Password": ["Şifre en az 8 karakter olmalıdır."]
    ///     }
    ///   }
    /// }
    /// </summary>
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;

        public ErrorPayload Error { get; set; } = new();

        /// <summary>Request izleme ID'si - log ile eşleştirmek için kullanılır</summary>
        public string TraceId { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Stack trace ve detaylı hata bilgisi.
        /// Sadece Development ortamında döner (Production'da null).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DeveloperMessage { get; set; }
    }

    /// <summary>Hata detaylarını içeren nesne</summary>
    public class ErrorPayload
    {
        /// <summary>Machine-readable hata kodu (ör: "validation_error")</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Kullanıcıya gösterilecek mesaj</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Alan bazlı doğrulama hataları. Sadece validation hatalarında dolu gelir.
        /// Key: alan adı, Value: hata mesajları dizisi
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string[]>? Details { get; set; }
    }
}
