using Core.Entities.Abstract;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Kullanıcıların abonelik durumlarını, sürelerini ve App Store işlem detaylarını takip eden veritabanı modeli.
    /// </summary>
    public class UserSubscription : IAuditableEntity
    {
        public int UserSubscriptionId { get; set; }
        
        public int UserId { get; set; }
        
        public string SubscriptionTier { get; set; } = "Free"; // "Free", "Premium", "Pro"
        
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndDate { get; set; }
        
        public bool IsActive { get; set; } = false;
        
        public string? OriginalTransactionId { get; set; }
        
        public string? ProductId { get; set; }
        
        public DateTime? PurchaseDate { get; set; }
        
        public DateTime? ExpirationDate { get; set; }
        
        public bool IsAutoRenewing { get; set; } = false;
        
        public string Status { get; set; } = "Free"; // "Free", "Active", "Expired", "Cancelled"
        
        public bool IsDeleted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
}
