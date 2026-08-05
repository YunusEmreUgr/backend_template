using Asp.Versioning;
using Business.Abstract;
using Core.Utilities.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    /// <summary>
    /// App Store uygulama içi satın alım ve abonelik doğrulamalarını yöneten Controller.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// Apple App Store'dan alınan Base64 formatındaki makbuzu doğrular ve kullanıcının aboneliğini günceller.
        /// </summary>
        [Authorize]
        [HttpPost("verify-receipt")]
        public async Task<IActionResult> VerifyReceipt([FromBody] VerifyReceiptRequest request)
        {
            if (string.IsNullOrEmpty(request.ReceiptData))
            {
                return BadRequest(new { success = false, message = "Makbuz verisi (ReceiptData) boş bırakılamaz." });
            }

            var userId = GetCurrentUserId();
            var result = await _subscriptionService.VerifyAppleReceiptAsync(userId, request.ReceiptData, request.Environment);
            
            if (result.Success)
            {
                return Ok(new { success = true, message = result.Message, data = result.Data });
            }

            return BadRequest(new { success = false, message = result.Message });
        }

        /// <summary>
        /// App Store Server Notifications V2 (Webhook) entegrasyonu.
        /// Apple sunucuları abonelik yenileme, iptal veya değişim işlemlerini buraya bildirir.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("apple-webhook")]
        public async Task<IActionResult> AppleWebhook([FromBody] AppleWebhookPayload payload)
        {
            if (string.IsNullOrEmpty(payload.SignedPayload))
            {
                return BadRequest(new { success = false, message = "SignedPayload parametresi eksik." });
            }

            var result = await _subscriptionService.HandleAppleWebhookAsync(payload.SignedPayload);
            if (result.Success)
            {
                return Ok(new { success = true, message = result.Message });
            }

            return BadRequest(new { success = false, message = result.Message });
        }

        /// <summary>
        /// Giriş yapan kullanıcının aktif abonelik bilgisini sorgular.
        /// </summary>
        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var userId = GetCurrentUserId();
            var result = await _subscriptionService.GetSubscriptionStatusAsync(userId);

            if (result.Success)
            {
                return Ok(new { success = true, data = result.Data });
            }

            return BadRequest(new { success = false, message = result.Message });
        }

        // ─── Yardımcı Metotlar ───────────────────────────────────────────────

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == "userid")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new UserFriendlyException("Oturum geçersiz. Lütfen tekrar giriş yapın.", ErrorCodes.AuthenticationFailed, StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>
    /// Makbuz doğrulama isteği modeli.
    /// </summary>
    public class VerifyReceiptRequest
    {
        public string ReceiptData { get; set; } = null!;
        public string? Environment { get; set; } // "Sandbox" veya "Production" (Opsiyonel)
    }

    /// <summary>
    /// Apple Webhook bildirim gövdesi.
    /// </summary>
    public class AppleWebhookPayload
    {
        public string SignedPayload { get; set; } = null!;
    }
}
