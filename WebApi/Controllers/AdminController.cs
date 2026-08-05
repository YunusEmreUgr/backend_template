using Asp.Versioning;
using Business.Abstract;
using Core.Entities.Concrete.Users;
using Core.Utilities.Exceptions;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    /// <summary>
    /// Yönetim paneli işlemlerini gerçekleştiren Controller sınıfı.
    /// Sadece Admin rolüne sahip kullanıcılar erişebilir.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserDal _userDal;
        private readonly IUserSubscriptionDal _userSubscriptionDal;
        private readonly IAppStoreTransactionDal _appStoreTransactionDal;
        private readonly IProductDal _productDal;
        private readonly IOperationClaimDal _operationClaimDal;
        private readonly IUserOperationClaimDal _userOperationClaimDal;
        private readonly IMemoryCache _cache;

        public AdminController(
            IUserDal userDal,
            IUserSubscriptionDal userSubscriptionDal,
            IAppStoreTransactionDal appStoreTransactionDal,
            IProductDal productDal,
            IOperationClaimDal operationClaimDal,
            IUserOperationClaimDal userOperationClaimDal,
            IMemoryCache cache)
        {
            _userDal = userDal;
            _userSubscriptionDal = userSubscriptionDal;
            _appStoreTransactionDal = appStoreTransactionDal;
            _productDal = productDal;
            _operationClaimDal = operationClaimDal;
            _userOperationClaimDal = userOperationClaimDal;
            _cache = cache;
        }

        /// <summary>
        /// Yönetim paneli ana sayfası için istatistikleri döner.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = (await _userDal.GetAllAsync()).Count;
            var subscriptions = await _userSubscriptionDal.GetAllAsync();
            var activeSubsCount = subscriptions.Count(s => s.IsActive);
            
            // MRR Hesaplama (Aylık Premium = 129.99, Yıllık Premium = 949.99 / 12)
            decimal mrr = 0;
            foreach (var sub in subscriptions.Where(s => s.IsActive))
            {
                if (sub.ProductId != null && sub.ProductId.Contains("yearly"))
                {
                    mrr += 949.99m / 12;
                }
                else
                {
                    mrr += 129.99m;
                }
            }

            var totalProducts = (await _productDal.GetAllAsync()).Count;

            // Son 5 yeni kayıt olan kullanıcı
            var allUsers = await _userDal.GetAllAsync();
            var recentUsers = allUsers
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new { u.UserId, u.FirstName, u.LastName, u.Email, u.CreatedAt })
                .ToList();

            // Son 6 ayın mock satış verileri (Grafik için)
            var salesData = new List<object>
            {
                new { month = "Şub", sales = Math.Round(mrr * 0.7m, 2) },
                new { month = "Mar", sales = Math.Round(mrr * 0.8m, 2) },
                new { month = "Nis", sales = Math.Round(mrr * 0.85m, 2) },
                new { month = "May", sales = Math.Round(mrr * 0.9m, 2) },
                new { month = "Haz", sales = Math.Round(mrr * 0.95m, 2) },
                new { month = "Tem", sales = Math.Round(mrr, 2) }
            };

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalUsers,
                    activeSubscriptions = activeSubsCount,
                    mrr = Math.Round(mrr, 2),
                    totalProducts,
                    recentUsers,
                    salesData
                }
            });
        }

        /// <summary>
        /// Kayıtlı kullanıcıları filtreleyerek ve sayfalayarak getirir.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var users = await _userDal.GetAllAsync();
            
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                users = users.Where(u => u.Email.ToLower().Contains(s) || 
                                         u.FirstName.ToLower().Contains(s) || 
                                         u.LastName.ToLower().Contains(s)).ToList();
            }

            var totalCount = users.Count;
            var items = users
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var usersWithClaims = new List<object>();
            foreach (var user in items)
            {
                var claims = await _userDal.GetClaimsAsync(user);
                var claimNames = claims.Select(c => c.OperationClaimName).ToList();
                usersWithClaims.Add(new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Status,
                    user.CreatedAt,
                    claims = claimNames
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    items = usersWithClaims,
                    totalCount,
                    pageNumber,
                    pageSize
                }
            });
        }

        /// <summary>
        /// Kullanıcının aktiflik durumunu değiştirir (Banlama/Aktifleştirme).
        /// </summary>
        [HttpPut("users/{userId}/status")]
        public async Task<IActionResult> ToggleUserStatus(int userId, [FromBody] ToggleStatusRequest request)
        {
            var user = await _userDal.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            user.Status = request.Status;
            user.UpdatedAt = DateTime.UtcNow;
            await _userDal.UpdateAsync(user);

            // Kullanıcının önbelleğe alınmış durum bilgisini temizle
            var cacheKey = $"UserStatus_{userId}";
            _cache.Remove(cacheKey);

            var actionText = request.Status ? "aktifleştirildi" : "engellendi";
            return Ok(new { success = true, message = $"Kullanıcı başarıyla {actionText}." });
        }

        /// <summary>
        /// Kullanıcı yetki ve rollerini günceller.
        /// </summary>
        [HttpPut("users/{userId}/claims")]
        public async Task<IActionResult> UpdateUserClaims(int userId, [FromBody] UpdateClaimsRequest request)
        {
            var user = await _userDal.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            // Mevcut rolleri çek
            var existingUserClaims = await _userOperationClaimDal.GetAllAsync(uoc => uoc.UserId == userId);

            // İstekle gelen yetki adlarının DB karşılıklarını bul
            var allClaims = await _operationClaimDal.GetAllAsync();
            var targetClaimIds = allClaims
                .Where(c => request.ClaimNames.Contains(c.OperationClaimName, StringComparer.OrdinalIgnoreCase))
                .Select(c => c.OperationClaimId)
                .ToList();

            // Silinecek yetkiler
            var claimsToRemove = existingUserClaims.Where(e => !targetClaimIds.Contains(e.OperationClaimId)).ToList();
            foreach (var c in claimsToRemove)
            {
                await _userOperationClaimDal.DeleteAsync(c);
            }

            // Eklenecek yetkiler
            var claimsToAdd = targetClaimIds.Where(id => !existingUserClaims.Any(e => e.OperationClaimId == id)).ToList();
            foreach (var id in claimsToAdd)
            {
                await _userOperationClaimDal.AddAsync(new UserOperationClaim
                {
                    UserId = userId,
                    OperationClaimId = id
                });
            }

            return Ok(new { success = true, message = "Kullanıcı rolleri başarıyla güncellendi." });
        }

        /// <summary>
        /// Abonelik ve satın alım işlem listesini getirir.
        /// </summary>
        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptions()
        {
            var subscriptions = await _userSubscriptionDal.GetAllAsync();
            var transactions = await _appStoreTransactionDal.GetAllAsync();
            var users = await _userDal.GetAllAsync();

            var subList = subscriptions
                .OrderByDescending(s => s.ExpirationDate)
                .Select(s => {
                    var u = users.FirstOrDefault(user => user.UserId == s.UserId);
                    return new {
                        s.UserSubscriptionId,
                        s.UserId,
                        userEmail = u?.Email ?? "bilinmeyen",
                        s.SubscriptionTier,
                        s.IsActive,
                        s.ProductId,
                        s.ExpirationDate,
                        s.Status
                    };
                }).ToList();

            var txList = transactions
                .OrderByDescending(t => t.PurchaseDate)
                .Select(t => {
                    var u = users.FirstOrDefault(user => user.UserId == t.UserId);
                    return new {
                        t.AppStoreTransactionId,
                        t.UserId,
                        userEmail = u?.Email ?? "bilinmeyen",
                        t.TransactionId,
                        t.ProductId,
                        t.PurchaseDate,
                        t.ExpirationDate,
                        t.Environment
                    };
                }).ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    subscriptions = subList,
                    transactions = txList
                }
            });
        }
    }

    public class ToggleStatusRequest
    {
        public bool Status { get; set; }
    }

    public class UpdateClaimsRequest
    {
        public List<string> ClaimNames { get; set; } = new();
    }
}
