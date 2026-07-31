using Serilog;

namespace Core.Utilities.Logging
{
    /// <summary>
    /// ILoggerService'in Serilog tabanlı implementasyonu.
    /// 
    /// Serilog'un statik Log.Logger'ını kullanır.
    /// Program.cs'te Log.Logger ayarlandıktan sonra bu servis doğru çalışır.
    /// 
    /// Log çıktıları:
    ///   - Konsol (Development)
    ///   - Dosya (logs/log-{tarih}.txt, her gün yeni dosya)
    ///   - appsettings.json'daki Serilog konfigürasyonu ile genişletilebilir
    /// 
    /// Servis kayıt: services.AddSingleton&lt;ILoggerService, SerilogLoggerService&gt;();
    /// </summary>
    public class SerilogLoggerService : ILoggerService
    {
        private readonly ILogger _logger;

        public SerilogLoggerService()
        {
            // Serilog'un global statik instance'ını kullan
            _logger = Log.Logger;
        }

        public void LogInfo(string message, params object[] args)
        {
            _logger.Information(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.Warning(message, args);
        }

        public void LogError(string message, Exception? exception = null, params object[] args)
        {
            if (exception != null)
                _logger.Error(exception, message, args);
            else
                _logger.Error(message, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.Debug(message, args);
        }
    }
}
