namespace Core.Utilities.Results
{
    /// <summary>
    /// Veri döndüren işlem sonuçları için generic interface.
    /// 
    /// Kullanım örneği:
    ///   IDataResult&lt;User&gt; loginResult = await _authService.Login(dto);
    ///   if (loginResult.Success) var user = loginResult.Data;
    /// </summary>
    /// <typeparam name="T">Döndürülen veri tipi</typeparam>
    public interface IDataResult<T> : IResult
    {
        T Data { get; }
    }
}
