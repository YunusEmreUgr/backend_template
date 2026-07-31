using Core.Entities.Abstract;

namespace Entities.Concrete
{
    /// <summary>
    /// Örnek Product entity'si.
    /// 
    /// Kendi entity'leriniz için bu sınıfı template olarak kullanın:
    ///   1. public class [EntityName] : IEntity { }
    ///   2. Properties ekleyin
    ///   3. Soft delete için IsDeleted, timestamp için CreatedAt/UpdatedAt ekleyin
    ///   4. AppDbContext'e DbSet ekleyin
    ///   5. OnModelCreating'de HasQueryFilter ve Index'leri tanımlayın
    /// 
    /// Soft Delete:
    ///   - IsDeleted = true → Silinmiş sayılır (HasQueryFilter otomatik filtreler)
    ///   - EfEntityRepositoryBase.DeleteAsync() IsDeleted'i otomatik set eder
    /// </summary>
    public class Product : IAuditableEntity
    {
        public int ProductId { get; set; }

        /// <summary>Ürün adı - benzersiz olmalıdır (business rule)</summary>
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>Fiyat - decimal precision: HasPrecision(18, 2) ile tanımlayın</summary>
        public decimal Price { get; set; }

        /// <summary>Stok adedi</summary>
        public int Stock { get; set; }

        /// <summary>Kategori FK (zorunlu)</summary>
        public int CategoryId { get; set; }

        /// <summary>Ürünü ekleyen kullanıcı FK</summary>
        public int UserId { get; set; }

        // ─── Soft Delete ─────────────────────────────────────────────────────
        /// <summary>
        /// true → Silindi (HasQueryFilter ile sorgulardan otomatik çıkar).
        /// EfEntityRepositoryBase.DeleteAsync() bu alanı set eder.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        // ─── Timestamp'ler ────────────────────────────────────────────────────
        /// <summary>
        /// Oluşturulma tarihi UTC.
        /// AppDbContext.SaveChangesAsync() otomatik set eder.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Güncellenme tarihi UTC.
        /// AppDbContext.SaveChangesAsync() otomatik set eder.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // ─── Navigation Properties (isteğe bağlı) ────────────────────────────
        // TODO: İlişkisel veri için navigation property'ler eklenebilir
        // public Category? Category { get; set; }
        // public User? User { get; set; }
    }
}
