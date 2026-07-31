using Microsoft.AspNetCore.Http;

namespace Core.Utilities.Exceptions
{
    /// <summary>
    /// Kullanıcıya gösterilebilir, kontrollü bir hata.
    /// 
    /// Bu exception ExceptionMiddleware tarafından yakalanır ve:
    ///   - Uygun HTTP status kodu döndürülür
    ///   - Hata kodu (machine-readable) döndürülür  
    ///   - Kullanıcıya dost mesaj gösterilir
    ///   - Loglanır
    /// 
    /// Kullanım örnekleri:
    ///   throw new UserFriendlyException("Email zaten kayıtlı.", ErrorCodes.Conflict, 409);
    ///   throw new UserFriendlyException("Yetersiz bakiye.", ErrorCodes.GeneralFailure, 400);
    ///   throw new UserFriendlyException("Bulunamadı.", ErrorCodes.ResourceNotFound, 404);
    /// </summary>
    public class UserFriendlyException : Exception
    {
        public UserFriendlyException(
            string message,
            string errorCode = ErrorCodes.GeneralFailure,
            int statusCode = StatusCodes.Status400BadRequest,
            IDictionary<string, string[]>? errors = null,
            Exception? innerException = null)
            : base(message, innerException)
        {
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? ErrorCodes.GeneralFailure : errorCode;
            StatusCode = statusCode;
            Errors = errors;
        }

        /// <summary>Machine-readable hata kodu (ErrorCodes sabitlerinden biri)</summary>
        public string ErrorCode { get; }

        /// <summary>HTTP yanıt durum kodu</summary>
        public int StatusCode { get; }

        /// <summary>Alan bazlı doğrulama hataları (FluentValidation benzeri)</summary>
        public IDictionary<string, string[]>? Errors { get; }
    }
}
