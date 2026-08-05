using Business.Abstract;
using Business.Constants;
using Core.Entities.Concrete.Users;
using Core.Utilities.Results;
using IResult = Core.Utilities.Results.IResult;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Concrete
{
    public class SubscriptionManager : ISubscriptionService
    {
        private readonly IUserSubscriptionDal _userSubscriptionDal;
        private readonly IAppStoreTransactionDal _appStoreTransactionDal;
        private readonly IUserOperationClaimDal _userOperationClaimDal;
        private readonly IOperationClaimDal _operationClaimDal;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SubscriptionManager(
            IUserSubscriptionDal userSubscriptionDal,
            IAppStoreTransactionDal appStoreTransactionDal,
            IUserOperationClaimDal userOperationClaimDal,
            IOperationClaimDal operationClaimDal,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _userSubscriptionDal = userSubscriptionDal;
            _appStoreTransactionDal = appStoreTransactionDal;
            _userOperationClaimDal = userOperationClaimDal;
            _operationClaimDal = operationClaimDal;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public async Task<IDataResult<UserSubscription>> VerifyAppleReceiptAsync(int userId, string receiptData, string? environment = null)
        {
            var sharedSecret = _configuration["SocialAuth:Apple:SharedSecret"] ?? "";

            // Development/Mock Bypasser
            if (receiptData.StartsWith("mock_", StringComparison.OrdinalIgnoreCase) || 
                receiptData == "fake_apple_receipt" || 
                string.IsNullOrEmpty(sharedSecret)) 
            {
                var productId = receiptData.StartsWith("mock_") ? receiptData.Substring(5) : "subscription_premium_yearly";
                var purchaseDate = DateTime.UtcNow;
                var expiresDate = productId.Contains("yearly") ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);

                var tier = productId.Contains("pro", StringComparison.OrdinalIgnoreCase) ? "Pro" : "Premium";

                // Create or update
                var subscription = await _userSubscriptionDal.GetAsync(us => us.UserId == userId);
                if (subscription == null)
                {
                    subscription = new UserSubscription
                    {
                        UserId = userId,
                        SubscriptionTier = tier,
                        StartDate = purchaseDate,
                        IsActive = true,
                        OriginalTransactionId = "mock_tx_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                        ProductId = productId,
                        PurchaseDate = purchaseDate,
                        ExpirationDate = expiresDate,
                        IsAutoRenewing = true,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _userSubscriptionDal.AddAsync(subscription);
                }
                else
                {
                    subscription.SubscriptionTier = tier;
                    subscription.IsActive = true;
                    subscription.OriginalTransactionId = "mock_tx_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                    subscription.ProductId = productId;
                    subscription.PurchaseDate = purchaseDate;
                    subscription.ExpirationDate = expiresDate;
                    subscription.Status = "Active";
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _userSubscriptionDal.UpdateAsync(subscription);
                }

                // Add to AppStoreTransaction log
                await _appStoreTransactionDal.AddAsync(new AppStoreTransaction
                {
                    UserId = userId,
                    TransactionId = subscription.OriginalTransactionId,
                    OriginalTransactionId = subscription.OriginalTransactionId,
                    ProductId = productId,
                    PurchaseDate = purchaseDate,
                    ExpirationDate = expiresDate,
                    Environment = "Sandbox",
                    RawPayload = "{\"mock\": true, \"note\": \"Simulated local bypass during development\"}",
                    CreatedAt = DateTime.UtcNow
                });

                // Update Premium Claim
                await UpdateUserPremiumRoleClaimAsync(userId, true);

                return new SuccessDataResult<UserSubscription>(subscription, "Makbuz başarıyla simüle edildi ve doğrulandı (Geliştirme Modu).", StatusCodes.Status200OK);
            }

            var payload = new
            {
                receipt_data = receiptData,
                password = sharedSecret,
                exclude_old_transactions = false
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            
            // 1. Prod URL dene
            var verifyUrl = "https://buy.itunes.apple.com/verifyReceipt";
            if (environment == "Sandbox")
            {
                verifyUrl = "https://sandbox.itunes.apple.com/verifyReceipt";
            }

            var response = await client.PostAsync(verifyUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                return new ErrorDataResult<UserSubscription>("Apple API ile iletişim kurulamadı.", StatusCodes.Status502BadGateway);
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetInt32();

            // 2. Eğer 21007 dönmüşse (Prod servisine Sandbox fişi gönderilmişse) Sandbox'a yönlendir
            if (status == 21007 && environment != "Sandbox")
            {
                verifyUrl = "https://sandbox.itunes.apple.com/verifyReceipt";
                response = await client.PostAsync(verifyUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    return new ErrorDataResult<UserSubscription>("Apple Sandbox API ile iletişim kurulamadı.", StatusCodes.Status502BadGateway);
                }
                responseString = await response.Content.ReadAsStringAsync();
                doc.Dispose();
                var newDoc = JsonDocument.Parse(responseString);
                return await ProcessAppleReceiptResponseAsync(userId, newDoc.RootElement, receiptData);
            }

            if (status != 0)
            {
                return new ErrorDataResult<UserSubscription>($"Apple doğrulama hatası. Hata Kodu: {status}", StatusCodes.Status400BadRequest);
            }

            return await ProcessAppleReceiptResponseAsync(userId, root, receiptData);
        }

        /// <inheritdoc/>
        public async Task<IResult> HandleAppleWebhookAsync(string signedPayload)
        {
            try
            {
                // App Store Server Notifications V2 JWS formatında gelir. Payload'ı çözümlüyoruz.
                var decodedNotification = DecodeJwsPayload(signedPayload);
                if (string.IsNullOrEmpty(decodedNotification))
                {
                    return new ErrorResult("Geçersiz bildirim yükü (SignedPayload çözümlenemedi).");
                }

                using var notificationDoc = JsonDocument.Parse(decodedNotification);
                var notificationRoot = notificationDoc.RootElement;

                // Bildirim tipi (örn: SUBSCRIBED, DID_RENEW, EXPIRED, vb.)
                var notificationType = notificationRoot.GetProperty("notificationType").GetString();
                var data = notificationRoot.GetProperty("data");
                
                // İşlem detaylarını içeren JWS (signedTransactionInfo) çözümlenir
                var signedTransactionInfo = data.GetProperty("signedTransactionInfo").GetString();
                if (string.IsNullOrEmpty(signedTransactionInfo))
                {
                    return new ErrorResult("Bildirim içerisinde işlem bilgisi (signedTransactionInfo) bulunamadı.");
                }

                var decodedTransaction = DecodeJwsPayload(signedTransactionInfo);
                using var transactionDoc = JsonDocument.Parse(decodedTransaction);
                var transactionRoot = transactionDoc.RootElement;

                // İşlem detayları
                var transactionId = transactionRoot.GetProperty("transactionId").GetString() ?? "";
                var originalTransactionId = transactionRoot.GetProperty("originalTransactionId").GetString() ?? "";
                var productId = transactionRoot.GetProperty("productId").GetString() ?? "";
                var environment = transactionRoot.GetProperty("environment").GetString() ?? "Production";

                var purchaseDateMs = transactionRoot.GetProperty("purchaseDate").GetInt64();
                var purchaseDate = DateTimeOffset.FromUnixTimeMilliseconds(purchaseDateMs).UtcDateTime;

                DateTime? expiresDate = null;
                if (transactionRoot.TryGetProperty("expiresDate", out var expiresProp))
                {
                    var expiresDateMs = expiresProp.GetInt64();
                    expiresDate = DateTimeOffset.FromUnixTimeMilliseconds(expiresDateMs).UtcDateTime;
                }

                // Bildirim tipine göre aboneliği eşleştir/güncelle
                // Not: Webhook'ta UserId direkt yer almaz. originalTransactionId üzerinden veritabanındaki kullanıcıyı eşleştiririz.
                var existingSub = await _userSubscriptionDal.GetAsync(us => us.OriginalTransactionId == originalTransactionId);
                if (existingSub == null)
                {
                    // Eğer veritabanında yoksa, webhook işlemini loglayıp atlayabiliriz ya da bekletebiliriz.
                    // Üretim ortamında genellikle kullanıcı önce uygulamada satın alım yapar, bu yüzden kayıt bulunur.
                    return new SuccessResult("Bildirim alındı fakat ilişkili abonelik kaydı veritabanında bulunamadı (İlk satın alım client tarafından doğrulanmalıdır).");
                }

                // Abonelik güncelleme
                existingSub.ProductId = productId;
                existingSub.PurchaseDate = purchaseDate;
                existingSub.ExpirationDate = expiresDate;
                existingSub.IsAutoRenewing = notificationType != "DID_CHANGE_RENEWAL_STATUS" || 
                                             (notificationRoot.TryGetProperty("subtype", out var subtypeProp) && subtypeProp.GetString() == "AUTO_RENEW_ENABLED");
                
                existingSub.UpdatedAt = DateTime.UtcNow;

                bool isActive = expiresDate == null || expiresDate > DateTime.UtcNow;
                existingSub.IsActive = isActive;

                if (notificationType == "EXPIRED" || notificationType == "REVOKED")
                {
                    existingSub.IsActive = false;
                    existingSub.Status = "Expired";
                }
                else
                {
                    existingSub.IsActive = isActive;
                    existingSub.Status = isActive ? "Active" : "Expired";
                }

                await _userSubscriptionDal.UpdateAsync(existingSub);

                // İşlem kaydını logla
                await _appStoreTransactionDal.AddAsync(new AppStoreTransaction
                {
                    UserId = existingSub.UserId,
                    TransactionId = transactionId,
                    OriginalTransactionId = originalTransactionId,
                    ProductId = productId,
                    PurchaseDate = purchaseDate,
                    ExpirationDate = expiresDate,
                    Environment = environment,
                    RawPayload = decodedTransaction,
                    CreatedAt = DateTime.UtcNow
                });

                // Kullanıcının Premium rol yetkisini güncelle
                await UpdateUserPremiumRoleClaimAsync(existingSub.UserId, existingSub.IsActive);

                return new SuccessResult("Webhook bildirimi başarıyla işlendi.");
            }
            catch (Exception ex)
            {
                return new ErrorResult($"Webhook işleme hatası: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<IDataResult<UserSubscription?>> GetSubscriptionStatusAsync(int userId)
        {
            var sub = await _userSubscriptionDal.GetAsync(us => us.UserId == userId);
            if (sub == null)
            {
                return new SuccessDataResult<UserSubscription?>(null, "Kullanıcının aktif bir abonelik kaydı bulunmamaktadır.");
            }

            // Süre kontrolünü yap ve pasifleşmişse veritabanını güncelle
            if (sub.IsActive && sub.ExpirationDate != null && sub.ExpirationDate < DateTime.UtcNow)
            {
                sub.IsActive = false;
                sub.Status = "Expired";
                sub.UpdatedAt = DateTime.UtcNow;
                await _userSubscriptionDal.UpdateAsync(sub);
                await UpdateUserPremiumRoleClaimAsync(userId, false);
            }

            return new SuccessDataResult<UserSubscription?>(sub, "Abonelik durumu başarıyla getirildi.");
        }

        // ─── Yardımcı Metotlar (Private Helpers) ───────────────────────────────

        private async Task<IDataResult<UserSubscription>> ProcessAppleReceiptResponseAsync(int userId, JsonElement root, string rawReceipt)
        {
            // Apple VerifyReceipt çıktısında 'latest_receipt_info' dizisi yer alır.
            if (!root.TryGetProperty("latest_receipt_info", out var latestReceiptInfo) || latestReceiptInfo.ValueKind != JsonValueKind.Array)
            {
                return new ErrorDataResult<UserSubscription>("Makbuz doğrulaması başarılı fakat abonelik işlemi bulunamadı.", StatusCodes.Status400BadRequest);
            }

            JsonElement? latestTransaction = null;
            long latestExpiresMs = 0;

            foreach (var transaction in latestReceiptInfo.EnumerateArray())
            {
                if (transaction.TryGetProperty("expires_date_ms", out var expiresMsProp))
                {
                    if (long.TryParse(expiresMsProp.GetString(), out long expiresMs))
                    {
                        if (expiresMs > latestExpiresMs)
                        {
                            latestExpiresMs = expiresMs;
                            latestTransaction = transaction;
                        }
                    }
                }
            }

            if (latestTransaction == null)
            {
                return new ErrorDataResult<UserSubscription>("Abonelik sona erme zamanı bulunamadı.", StatusCodes.Status400BadRequest);
            }

            var transactionId = latestTransaction.Value.GetProperty("transaction_id").GetString() ?? "";
            var originalTransactionId = latestTransaction.Value.GetProperty("original_transaction_id").GetString() ?? "";
            var productId = latestTransaction.Value.GetProperty("product_id").GetString() ?? "";
            
            var purchaseDateMsStr = latestTransaction.Value.GetProperty("purchase_date_ms").GetString();
            long.TryParse(purchaseDateMsStr, out long purchaseDateMs);
            var purchaseDate = DateTimeOffset.FromUnixTimeMilliseconds(purchaseDateMs).UtcDateTime;
            var expiresDate = DateTimeOffset.FromUnixTimeMilliseconds(latestExpiresMs).UtcDateTime;

            bool isActive = expiresDate > DateTime.UtcNow;
            var tier = productId.Contains("pro", StringComparison.OrdinalIgnoreCase) ? "Pro" : "Premium";

            // Mevcut aboneliği güncelle veya yeni oluştur
            var subscription = await _userSubscriptionDal.GetAsync(us => us.UserId == userId);
            if (subscription == null)
            {
                subscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionTier = tier,
                    StartDate = purchaseDate,
                    IsActive = isActive,
                    OriginalTransactionId = originalTransactionId,
                    ProductId = productId,
                    PurchaseDate = purchaseDate,
                    ExpirationDate = expiresDate,
                    IsAutoRenewing = true,
                    Status = isActive ? "Active" : "Expired",
                    CreatedAt = DateTime.UtcNow
                };
                await _userSubscriptionDal.AddAsync(subscription);
            }
            else
            {
                subscription.SubscriptionTier = tier;
                subscription.IsActive = isActive;
                subscription.OriginalTransactionId = originalTransactionId;
                subscription.ProductId = productId;
                subscription.PurchaseDate = purchaseDate;
                subscription.ExpirationDate = expiresDate;
                subscription.Status = isActive ? "Active" : "Expired";
                subscription.UpdatedAt = DateTime.UtcNow;
                await _userSubscriptionDal.UpdateAsync(subscription);
            }

            // İşlemi kaydet (Ödeme geçmişi için)
            var existingTx = await _appStoreTransactionDal.GetAsync(t => t.TransactionId == transactionId);
            if (existingTx == null)
            {
                var environment = root.TryGetProperty("environment", out var envProp) ? envProp.GetString() : "Production";
                await _appStoreTransactionDal.AddAsync(new AppStoreTransaction
                {
                    UserId = userId,
                    TransactionId = transactionId,
                    OriginalTransactionId = originalTransactionId,
                    ProductId = productId,
                    PurchaseDate = purchaseDate,
                    ExpirationDate = expiresDate,
                    Environment = environment ?? "Production",
                    RawPayload = latestTransaction.Value.ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Kullanıcının Premium rol yetkisini güncelle
            await UpdateUserPremiumRoleClaimAsync(userId, isActive);

            return new SuccessDataResult<UserSubscription>(subscription, "Makbuz başarıyla doğrulandı.", StatusCodes.Status200OK);
        }

        private async Task UpdateUserPremiumRoleClaimAsync(int userId, bool shouldHavePremium)
        {
            var premiumClaim = await _operationClaimDal.GetAsync(oc => oc.OperationClaimName == "Premium");
            if (premiumClaim == null) return;

            var userClaimRelation = await _userOperationClaimDal.GetAsync(uoc => 
                uoc.UserId == userId && uoc.OperationClaimId == premiumClaim.OperationClaimId);

            if (shouldHavePremium)
            {
                if (userClaimRelation == null)
                {
                    await _userOperationClaimDal.AddAsync(new UserOperationClaim
                    {
                        UserId = userId,
                        OperationClaimId = premiumClaim.OperationClaimId
                    });
                }
            }
            else
            {
                if (userClaimRelation != null)
                {
                    await _userOperationClaimDal.DeleteAsync(userClaimRelation);
                }
            }
        }

        private string DecodeJwsPayload(string jwtToken)
        {
            try
            {
                var parts = jwtToken.Split('.');
                if (parts.Length < 2) return string.Empty;
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                var bytes = Convert.FromBase64String(payload);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
