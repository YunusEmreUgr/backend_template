namespace Core.Utilities.Results
{
    /// <summary>
    /// Veri döndüren sonuç sınıflarının base class'ı.
    /// Result'tan miras alır, Data özelliği ekler.
    /// </summary>
    public class DataResult<T> : Result, IDataResult<T>
    {
        public DataResult(T data, bool success, string message, int statusCode = 0, string? errorCode = null)
            : base(success, message, statusCode, errorCode)
        {
            Data = data;
        }

        public DataResult(T data, bool success, int statusCode = 0, string? errorCode = null)
            : base(success, statusCode, errorCode)
        {
            Data = data;
        }

        public T Data { get; }
    }
}
