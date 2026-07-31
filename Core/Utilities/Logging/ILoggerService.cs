namespace Core.Utilities.Logging
{
    /// <summary>
    /// Uygulama genelinde loglama soyutlama interface'i.
    /// 
    /// Bu interface sayesinde loglama implementasyonu (Serilog, NLog, vb.)
    /// herhangi bir değişiklik yapmadan değiştirilebilir.
    /// 
    /// Servis kayıt: services.AddSingleton&lt;ILoggerService, SerilogLoggerService&gt;();
    /// 
    /// Kullanım:
    ///   _logger.LogInfo("Kullanıcı giriş yaptı. UserId: {UserId}", userId);
    ///   _logger.LogError("Hata oluştu.", exception, extraData);
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>Bilgi mesajı loglar. Başarılı işlemler, önemli olaylar için.</summary>
        void LogInfo(string message, params object[] args);

        /// <summary>Uyarı mesajı loglar. Dikkat edilmesi gereken ama kritik olmayan durumlar için.</summary>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Hata mesajı loglar.
        /// exception parametresi null ise sadece mesaj loglanır.
        /// </summary>
        void LogError(string message, Exception? exception = null, params object[] args);

        /// <summary>Debug mesajı loglar. Sadece Development ortamında kullanılır.</summary>
        void LogDebug(string message, params object[] args);
    }
}
