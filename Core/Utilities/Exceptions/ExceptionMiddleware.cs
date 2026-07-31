using System.Diagnostics;
using System.Security;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Core.Utilities.Logging;

namespace Core.Utilities.Exceptions
{
    /// <summary>
    /// Global hata yakalama middleware'i.
    /// 
    /// Uygulama genelinde tüm hataları yakalar ve standart API yanıtı olarak döner.
    /// Program.cs'te pipeline'ın EN BAŞINA eklenmeli:
    ///   app.UseMiddleware&lt;ExceptionMiddleware&gt;();
    /// 
    /// Desteklenen exception tipleri:
    ///   - ValidationException (FluentValidation)  → 400 Bad Request
    ///   - UserFriendlyException                   → İlgili HTTP status kodu
    ///   - UnauthorizedAccessException             → 401 Unauthorized
    ///   - SecurityException                       → 403 Forbidden
    ///   - KeyNotFoundException                    → 404 Not Found
    ///   - Exception (diğerleri)                   → 500 Internal Server Error
    /// 
    /// Tüm hatalar ILoggerService ile loglanır.
    /// Hata detayı (stack trace) sadece Development ortamında döner.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerService _loggerService;
        private readonly IHostEnvironment _hostEnvironment;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILoggerService loggerService,
            IHostEnvironment hostEnvironment)
        {
            _next = next;
            _loggerService = loggerService;
            _hostEnvironment = hostEnvironment;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // TraceId: loglar ile request'i eşleştirmek için kullanılır
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            var statusCode = StatusCodes.Status500InternalServerError;
            var errorCode = ErrorCodes.UnexpectedError;
            var fallbackMessage = "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
            IDictionary<string, string[]>? details = null;

            // Exception tipine göre HTTP yanıtını belirle
            switch (exception)
            {
                // FluentValidation doğrulama hataları
                case ValidationException validationException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = ErrorCodes.ValidationError;
                    fallbackMessage = "Lütfen formdaki hatalı alanları düzeltin.";
                    // Hataları alan bazında grupla
                    details = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(e => e.ErrorMessage).Distinct().ToArray());
                    break;

                // Kontrollü iş mantığı hataları
                case UserFriendlyException userFriendlyException:
                    statusCode = userFriendlyException.StatusCode;
                    errorCode = userFriendlyException.ErrorCode;
                    fallbackMessage = userFriendlyException.Message;
                    details = userFriendlyException.Errors;
                    break;

                // Kimlik doğrulama hatası
                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    errorCode = ErrorCodes.AuthenticationFailed;
                    fallbackMessage = "Lütfen oturum açtıktan sonra tekrar deneyin.";
                    break;

                // Yetkilendirme hatası
                case SecurityException:
                    statusCode = StatusCodes.Status403Forbidden;
                    errorCode = ErrorCodes.AccessDenied;
                    fallbackMessage = "Bu işlemi gerçekleştirmek için yetkiniz yok.";
                    break;

                // Kaynak bulunamadı
                case KeyNotFoundException keyNotFoundException:
                    statusCode = StatusCodes.Status404NotFound;
                    errorCode = ErrorCodes.ResourceNotFound;
                    fallbackMessage = string.IsNullOrWhiteSpace(keyNotFoundException.Message)
                        ? "Aradığınız kayıt bulunamadı."
                        : keyNotFoundException.Message;
                    break;

                // Diğer tüm hatalar → 500 Internal Server Error
                // (statusCode, errorCode, fallbackMessage varsayılan değerleriyle kalır)
            }

            // Production'da developer detayları gizle
            var developerMessage = _hostEnvironment.IsDevelopment() ? exception.ToString() : null;

            var errorResponse = new ErrorResponse
            {
                TraceId = traceId,
                DeveloperMessage = developerMessage,
                Error =
                {
                    Code = errorCode,
                    Message = fallbackMessage,
                    Details = details
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            // Hataları logla (TraceId ile takip edilebilir)
            _loggerService.LogError(
                "Unhandled exception. TraceId: {TraceId} | StatusCode: {StatusCode} | Code: {ErrorCode}",
                exception,
                traceId,
                statusCode,
                errorCode);

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(errorResponse, options);

            return context.Response.WriteAsync(json);
        }
    }
}
