namespace Core.Utilities.Results
{
    /// <summary>
    /// İş katmanı metodlarının geri dönüş tipi.
    /// 
    /// Her servis metodu bir IResult döner:
    ///   Success → İşlem başarılı mı?
    ///   Message → Kullanıcıya gösterilecek mesaj
    ///   StatusCode → HTTP status kodu (200, 201, 400, 401, 404, vb.)
    ///   ErrorCode → Programatik hata kodu (machine-readable, ör: "resource_not_found")
    /// 
    /// Veri döndürmek için IDataResult&lt;T&gt; kullanılır.
    /// </summary>
    public interface IResult
    {
        bool Success { get; }
        string Message { get; }
        int StatusCode { get; }
        string ErrorCode { get; }
    }
}
