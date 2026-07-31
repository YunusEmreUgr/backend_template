using System;

namespace Core.Entities.Abstract
{
    /// <summary>
    /// Veritabanı tablolarında oluşturulma ve güncellenme tarihlerinin otomatik olarak
    /// takip edilmesini (Audit) sağlayan arayüz.
    /// </summary>
    public interface IAuditableEntity : IEntity
    {
        /// <summary>Oluşturulma tarihi (UTC)</summary>
        DateTime CreatedAt { get; set; }

        /// <summary>Son güncellenme tarihi (UTC)</summary>
        DateTime? UpdatedAt { get; set; }
    }
}
