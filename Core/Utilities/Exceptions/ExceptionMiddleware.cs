using System.Diagnostics;
using System.Security;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Core.Utilities.Logging;
using Microsoft.Extensions.Localization;
using Core.Resources;

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
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILoggerService loggerService,
            IHostEnvironment hostEnvironment,
            IStringLocalizer<SharedResource> localizer)
        {
            _next = next;
            _loggerService = loggerService;
            _hostEnvironment = hostEnvironment;
            _localizer = localizer;
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
            var fallbackMessage = _localizer["UnexpectedError"].Value != "UnexpectedError" 
                ? _localizer["UnexpectedError"].Value 
                : "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
            IDictionary<string, string[]>? details = null;

            // Exception tipine göre HTTP yanıtını belirle
            switch (exception)
            {
                // FluentValidation doğrulama hataları
                case ValidationException validationException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = ErrorCodes.ValidationError;
                    fallbackMessage = _localizer["ValidationError"].Value != "ValidationError" 
                        ? _localizer["ValidationError"].Value 
                        : "Lütfen formdaki hatalı alanları düzeltin.";
                    
                    // Hataları alan bazında grupla ve hata mesajlarını yerelleştir
                    details = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(e => _localizer[e.ErrorMessage].Value).Distinct().ToArray());
                    break;

                // Kontrollü iş mantığı hataları
                case UserFriendlyException userFriendlyException:
                    statusCode = userFriendlyException.StatusCode;
                    errorCode = userFriendlyException.ErrorCode;
                    var localizedMsg = _localizer[userFriendlyException.Message].Value;
                    fallbackMessage = (!string.IsNullOrWhiteSpace(localizedMsg) && localizedMsg != userFriendlyException.Message)
                        ? localizedMsg
                        : userFriendlyException.Message;
                    details = userFriendlyException.Errors;
                    break;

                // Kimlik doğrulama hatası
                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    errorCode = ErrorCodes.AuthenticationFailed;
                    fallbackMessage = _localizer["AuthenticationFailed"].Value != "AuthenticationFailed" 
                        ? _localizer["AuthenticationFailed"].Value 
                        : "Lütfen oturum açtıktan sonra tekrar deneyin.";
                    break;

                // Yetkilendirme hatası
                case SecurityException:
                    statusCode = StatusCodes.Status403Forbidden;
                    errorCode = ErrorCodes.AccessDenied;
                    fallbackMessage = _localizer["AccessDenied"].Value != "AccessDenied" 
                        ? _localizer["AccessDenied"].Value 
                        : "Bu işlemi gerçekleştirmek için yetkiniz yok.";
                    break;

                // Kaynak bulunamadı
                case KeyNotFoundException keyNotFoundException:
                    statusCode = StatusCodes.Status404NotFound;
                    errorCode = ErrorCodes.ResourceNotFound;
                    fallbackMessage = string.IsNullOrWhiteSpace(keyNotFoundException.Message)
                        ? (_localizer["ResourceNotFound"].Value != "ResourceNotFound" ? _localizer["ResourceNotFound"].Value : "Aradığınız kayıt bulunamadı.")
                        : _localizer[keyNotFoundException.Message].Value;
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
