using Core.Utilities.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Core.Utilities.Results
{
    /// <summary>
    /// IResult'un temel implementasyonu.
    /// 
    /// Otomatik varsayılanlar:
    ///   - statusCode belirtilmezse: Success=true → 200, Success=false → 400
    ///   - errorCode belirtilmezse: Success=true → string.Empty, Success=false → "general_failure"
    /// 
    /// Kullanım örnekleri:
    ///   new Result(true)                           → 200 OK
    ///   new Result(false, "Hata mesajı")           → 400 Bad Request
    ///   new Result(false, "Bulunamadı", 404, ErrorCodes.ResourceNotFound)
    /// </summary>
    public class Result : IResult
    {
        public Result(bool success, string message, int statusCode = 0, string? errorCode = null)
            : this(success, statusCode, errorCode)
        {
            Message = message;
        }

        public Result(bool success, int statusCode = 0, string? errorCode = null)
        {
            Success = success;

            // StatusCode otomatik belirleme
            StatusCode = statusCode == 0
                ? (success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest)
                : statusCode;

            // ErrorCode otomatik belirleme
            ErrorCode = success
                ? string.Empty
                : (!string.IsNullOrWhiteSpace(errorCode) ? errorCode : ErrorCodes.GeneralFailure);
        }

        public bool Success { get; }
        public string Message { get; } = string.Empty;
        public int StatusCode { get; }
        public string ErrorCode { get; }
    }
}
