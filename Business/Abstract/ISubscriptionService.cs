using Core.Utilities.Results;
using Entities.Concrete;
using System.Threading.Tasks;

namespace Business.Abstract
{
    /// <summary>
    /// App Store abonelik ve satın alım işlemlerinin iş mantığını yöneten servis arayüzü.
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Apple App Store'dan gelen makbuzu (receipt) doğrular ve kullanıcının aboneliğini günceller.
        /// </summary>
        Task<IDataResult<UserSubscription>> VerifyAppleReceiptAsync(int userId, string receiptData, string? environment = null);

        /// <summary>
        /// Apple App Store Server Notifications V2 (Webhook) bildirilerini işler.
        /// </summary>
        Task<IResult> HandleAppleWebhookAsync(string signedPayload);

        /// <summary>
        /// Kullanıcının aktif abonelik detaylarını getirir.
        /// </summary>
        Task<IDataResult<UserSubscription?>> GetSubscriptionStatusAsync(int userId);
    }
}
