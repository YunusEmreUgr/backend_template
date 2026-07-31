namespace Core.Utilities.Results
{
    /// <summary>
    /// Başarılı işlem sonucu - veri döndürür.
    /// 
    /// Kullanım örnekleri:
    ///   return new SuccessDataResult&lt;User&gt;(user, "Kullanıcı bulundu");
    ///   return new SuccessDataResult&lt;List&lt;Product&gt;&gt;(products, 200);
    ///   return new SuccessDataResult&lt;Product&gt;(product, "Eklendi", 201);
    /// </summary>
    public class SuccessDataResult<T> : DataResult<T>
    {
        public SuccessDataResult(T data, string message, int statusCode = 0)
            : base(data, true, message, statusCode) { }

        public SuccessDataResult(T data, int statusCode = 0)
            : base(data, true, statusCode) { }

        public SuccessDataResult(string message, int statusCode = 0)
            : base(default!, true, message, statusCode) { }

        public SuccessDataResult(int statusCode = 0)
            : base(default!, true, statusCode) { }
    }
}
