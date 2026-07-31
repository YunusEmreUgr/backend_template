using Core.Utilities.Exceptions;

namespace Core.Utilities.Results
{
    /// <summary>
    /// Başarısız işlem sonucu - veri döndürür (genellikle null/default).
    /// 
    /// Kullanım örnekleri:
    ///   return new ErrorDataResult&lt;User&gt;("Kullanıcı bulunamadı", 404, ErrorCodes.ResourceNotFound);
    ///   return new ErrorDataResult&lt;Product&gt;("Yetki hatası", 403, ErrorCodes.AccessDenied);
    /// </summary>
    public class ErrorDataResult<T> : DataResult<T>
    {
        public ErrorDataResult(T data, string message, int statusCode = 0, string? errorCode = null)
            : base(data, false, message, statusCode, errorCode ?? ErrorCodes.GeneralFailure) { }

        public ErrorDataResult(T data, int statusCode = 0, string? errorCode = null)
            : base(data, false, statusCode, errorCode ?? ErrorCodes.GeneralFailure) { }

        public ErrorDataResult(string message, int statusCode = 0, string? errorCode = null)
            : base(default!, false, message, statusCode, errorCode ?? ErrorCodes.GeneralFailure) { }

        public ErrorDataResult(int statusCode = 0, string? errorCode = null)
            : base(default!, false, statusCode, errorCode ?? ErrorCodes.GeneralFailure) { }
    }
}
