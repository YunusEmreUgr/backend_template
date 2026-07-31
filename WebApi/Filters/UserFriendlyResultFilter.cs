using Core.Utilities.Exceptions;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.Filters
{
    /// <summary>
    /// Controller'lardan dönen başarısız (400+ status kodlu) sonuçları yakalar ve
    /// bunları UserFriendlyException'a dönüştürerek fırlatır.
    /// 
    /// Neden Kullanılır?
    ///   Controller'larda "if(!result.Success) return BadRequest(result);" yazıldığında
    ///   hata çıktısı standart API ErrorResponse formatında olmayabilir.
    ///   Bu filtre sayesinde, başarısız tüm ObjectResult veya StatusCodeResult'lar
    ///   UserFriendlyException'a çevrilerek ExceptionMiddleware tarafından yakalanır ve
    ///   tek bir standart JSON hata formatına dönüştürülür.
    /// </summary>
    public class UserFriendlyResultFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();

            // Eğer bir exception zaten fırlatıldıysa filtreyi pas geç
            if (executedContext.Exception != null)
            {
                return;
            }

            if (executedContext.Result is ObjectResult objectResult)
            {
                var statusCode = ResolveStatusCode(objectResult.StatusCode, objectResult);
                if (statusCode >= StatusCodes.Status400BadRequest)
                {
                    if (TryResolveMessage(objectResult.Value, statusCode, out var exception))
                    {
                        throw exception; // ExceptionMiddleware yakalayacak
                    }
                }
            }
            else if (executedContext.Result is ForbidResult)
            {
                throw CreateException(StatusCodes.Status403Forbidden, null);
            }
            else if (executedContext.Result is StatusCodeResult statusCodeResult)
            {
                var statusCode = statusCodeResult.StatusCode;
                if (statusCode >= StatusCodes.Status400BadRequest)
                {
                    throw CreateException(statusCode, null);
                }
            }
        }

        private static bool TryResolveMessage(object? value, int statusCode, out UserFriendlyException exception)
        {
            if (value is Core.Utilities.Results.IResult result)
            {
                if (result.Success)
                {
                    exception = default!;
                    return false;
                }

                var message = string.IsNullOrWhiteSpace(result.Message)
                    ? GetDefaultMessage(statusCode)
                    : result.Message;

                exception = CreateException(statusCode, message);
                return true;
            }

            if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                exception = CreateException(statusCode, stringValue);
                return true;
            }

            exception = CreateException(statusCode, GetDefaultMessage(statusCode));
            return true;
        }

        private static UserFriendlyException CreateException(int statusCode, string? message)
        {
            var fallbackMessage = string.IsNullOrWhiteSpace(message)
                ? GetDefaultMessage(statusCode)
                : message;

            return new UserFriendlyException(
                fallbackMessage,
                MapErrorCode(statusCode),
                statusCode);
        }

        private static int ResolveStatusCode(int? statusCode, ObjectResult objectResult)
        {
            if (statusCode.HasValue) return statusCode.Value;

            return objectResult switch
            {
                BadRequestObjectResult => StatusCodes.Status400BadRequest,
                NotFoundObjectResult => StatusCodes.Status404NotFound,
                UnauthorizedObjectResult => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest
            };
        }

        private static string MapErrorCode(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => ErrorCodes.ValidationError,
                StatusCodes.Status401Unauthorized => ErrorCodes.AuthenticationFailed,
                StatusCodes.Status403Forbidden => ErrorCodes.AccessDenied,
                StatusCodes.Status404NotFound => ErrorCodes.ResourceNotFound,
                StatusCodes.Status429TooManyRequests => ErrorCodes.TooManyRequests,
                _ => ErrorCodes.GeneralFailure
            };
        }

        private static string GetDefaultMessage(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "Geçersiz veya hatalı bir işlem gerçekleştirdiniz.",
                StatusCodes.Status401Unauthorized => "Lütfen oturum açtıktan sonra tekrar deneyin.",
                StatusCodes.Status403Forbidden => "Bu işlemi gerçekleştirmek için yetkiniz yok.",
                StatusCodes.Status404NotFound => "Aradığınız kayıt bulunamadı.",
                StatusCodes.Status429TooManyRequests => "Çok kısa sürede çok fazla deneme yaptınız. Lütfen bekleyin.",
                _ => "İşleminiz tamamlanamadı. Lütfen tekrar deneyin."
            };
        }
    }
}
