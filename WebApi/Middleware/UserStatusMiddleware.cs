using Business.Abstract;
using Core.Utilities.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace WebApi.Middleware
{
    /// <summary>
    /// Kimlik doğrulamış (Authenticated) her istekte kullanıcının ban/askı durumunu kontrol eder.
    /// Performans kaybını önlemek için IMemoryCache ile 5 dakikalık TTL (Time-To-Live) cache kullanır.
    /// 
    /// Çalışma Şekli:
    ///   1. İstek atanmış bir kullanıcı varsa token'dan UserId claim'ini okur.
    ///   2. Cache'de "UserStatus_UserId" key'i var mı bakar.
    ///   3. Yoksa, DB'den (IUserService) kullanıcının son durumunu alır ve cache'e kaydeder.
    ///   4. Kullanıcının Status = false (banlı) ise isteği durdurur ve 401 Unauthorized döner.
    /// </summary>
    public class UserStatusMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILoggerService _logger;

        public UserStatusMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILoggerService logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserService userService)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("userid") 
                                  ?? context.User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var cacheKey = $"UserStatus_{userId}";

                    // ⚡ Performans: Durumu cache'den oku
                    if (!_cache.TryGetValue(cacheKey, out bool? userStatus))
                    {
                        // Cache'de yoksa veritabanından taze çek
                        var user = await userService.GetById(userId);
                        if (user != null)
                        {
                            userStatus = user.Status;
                            // 5 dakika boyunca ban kontrolü için veritabanına gitme
                            _cache.Set(cacheKey, userStatus, TimeSpan.FromMinutes(5));
                        }
                    }

                    // Kullanıcı askıya alınmışsa (Status = false) isteği sonlandır
                    if (userStatus == false)
                    {
                        _logger.LogWarning("Askıya alınmış hesap erişim denemesi engellendi. UserId: {UserId}", userId);

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsJsonAsync(new
                        {
                            success = false,
                            error = new
                            {
                                code = "user_banned",
                                message = "Hesabınız askıya alınmıştır. Lütfen yönetici ile iletişime geçin."
                            }
                        });

                        return; // Request pipeline'ı sonlandır (Controller'a gitmez)
                    }
                }
            }

            await _next(context); // Bir sonraki middleware'e geç
        }
    }

    /// <summary>
    /// Kullanıcı ban/unban durumları güncellendiğinde cache'i temizlemek için yardımcı helper sınıfı.
    /// </summary>
    public static class UserStatusCacheHelper
    {
        /// <summary>Kullanıcının önbellekteki ban durumunu temizler.</summary>
        public static void ClearUserStatusCache(IMemoryCache cache, int userId)
        {
            var cacheKey = $"UserStatus_{userId}";
            cache.Remove(cacheKey);
        }
    }
}
