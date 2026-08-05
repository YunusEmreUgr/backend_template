using Core.Entities.Abstract;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// App Store'dan gelen ödeme ve abonelik işlemlerinin geçmişini tutan veritabanı denetim (audit) tablosu.
    /// </summary>
    public class AppStoreTransaction : IAuditableEntity
    {
        public int AppStoreTransactionId { get; set; }
        
        public int UserId { get; set; }
        
        public string TransactionId { get; set; } = null!;
        
        public string OriginalTransactionId { get; set; } = null!;
        
        public string ProductId { get; set; } = null!;
        
        public DateTime PurchaseDate { get; set; }
        
        public DateTime? ExpirationDate { get; set; }
        
        public string Environment { get; set; } = "Production"; // "Sandbox", "Production"
        
        public string? RawPayload { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
}
