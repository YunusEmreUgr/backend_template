using Core.Utilities.Exceptions;

namespace Core.Utilities.Results
{
    /// <summary>
    /// Başarısız işlem sonucu - veri döndürmez.
    /// 
    /// Kullanım örnekleri:
    ///   return new ErrorResult("Hata oluştu");
    ///   return new ErrorResult("Bulunamadı", 404, ErrorCodes.ResourceNotFound);
    /// </summary>
    public class ErrorResult : Result
    {
        public ErrorResult() : base(false) { }
        public ErrorResult(string message) : base(false, message) { }
        public ErrorResult(string message, int statusCode) : base(false, message, statusCode) { }
        public ErrorResult(string message, int statusCode, string errorCode)
            : base(false, message, statusCode, errorCode) { }
        public ErrorResult(int statusCode, string errorCode)
            : base(false, statusCode, errorCode) { }
    }
}
