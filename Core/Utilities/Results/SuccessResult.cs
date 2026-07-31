namespace Core.Utilities.Results
{
    /// <summary>
    /// Başarılı işlem sonucu - veri döndürmez.
    /// 
    /// Kullanım örnekleri:
    ///   return new SuccessResult();                    // 200 OK
    ///   return new SuccessResult("Başarıyla silindi"); // 200 OK, mesajlı
    ///   return new SuccessResult(StatusCodes.Status201Created); // 201 Created
    /// </summary>
    public class SuccessResult : Result
    {
        public SuccessResult() : base(true) { }
        public SuccessResult(string message) : base(true, message) { }
        public SuccessResult(int statusCode) : base(true, statusCode) { }
        public SuccessResult(string message, int statusCode) : base(true, message, statusCode) { }
    }
}
