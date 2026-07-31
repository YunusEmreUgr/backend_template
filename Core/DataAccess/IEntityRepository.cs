using Core.Entities.Abstract;
using System.Linq.Expressions;
using Core.Utilities.Results;

namespace Core.DataAccess
{
    /// <summary>
    /// Tüm DAL (Data Access Layer) arayüzlerinin miras alacağı generic repository interface.
    /// 
    /// SOLID prensiplerinden Interface Segregation ve Dependency Inversion'ı uygular.
    /// Business katmanı somut DAL implementasyonuna değil bu arayüze bağımlıdır.
    /// 
    /// Generic parametreler:
    ///   T → IEntity implement eden herhangi bir entity sınıfı
    /// 
    /// Tüm metodlar async'tir - I/O işlemlerinde thread bloklaması yaşanmaz.
    /// </summary>
    /// <typeparam name="T">IEntity implement eden entity sınıfı</typeparam>
    public interface IEntityRepository<T> where T : class, IEntity, new()
    {
        // ─── Okuma Metodları ─────────────────────────────────────────────────

        /// <summary>
        /// Filtreye uyan tüm kayıtları döner.
        /// filter null ise tüm kayıtları döner.
        /// includes ile navigation property'ler eager loading ile yüklenir.
        /// </summary>
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null,
            params Expression<Func<T, object>>[] includes);

        /// <summary>Filtreye uyan tek kayıt döner. Bulunamazsa null.</summary>
        Task<T?> GetAsync(Expression<Func<T, bool>> filter);

        /// <summary>Primary key'e göre kayıt getirir.</summary>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Include'lu tek kayıt getirme.
        /// Navigation property'lere ihtiyaç duyulduğunda kullanılır.
        /// </summary>
        Task<T?> GetWithIncludesAsync(Expression<Func<T, bool>> filter,
            params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Include'lu listeleme.
        /// Navigation property'lere ihtiyaç duyulduğunda kullanılır.
        /// </summary>
        Task<List<T>> GetAllWithIncludesAsync(Expression<Func<T, bool>>? filter = null,
            params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Sayfalama ile listeleme.
        /// Büyük veri setlerinde performans için kullanılır.
        /// Default: CreatedAt alanına göre desc sıralama.
        /// </summary>
        Task<List<T>> GetPagedAsync(Expression<Func<T, bool>>? filter = null,
            int pageNumber = 1, int pageSize = 10,
            params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Gelişmiş sayfalama ile listeleme.
        /// Toplam kayıt sayısı gibi meta bilgileri içeren PaginatedList nesnesi döner.
        /// </summary>
        Task<PaginatedList<T>> GetPaginatedAsync(Expression<Func<T, bool>>? filter = null,
            int pageNumber = 1, int pageSize = 10,
            params Expression<Func<T, object>>[] includes);

        // ─── Sayım ve Kontrol Metodları ───────────────────────────────────────

        /// <summary>Filtreye uyan kayıt sayısını döner.</summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

        /// <summary>Filtreye uyan herhangi bir kayıt var mı?</summary>
        Task<bool> AnyAsync(Expression<Func<T, bool>> filter);

        // ─── Yazma Metodları ─────────────────────────────────────────────────

        /// <summary>Yeni kayıt ekler ve SaveChanges çağırır.</summary>
        Task AddAsync(T entity);

        /// <summary>Mevcut kaydı günceller ve SaveChanges çağırır.</summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Kaydı siler.
        /// Entity'de IsDeleted alanı varsa → soft delete (silinmiş işaretlenir)
        /// Entity'de IsDeleted yoksa → hard delete (fiziksel silme)
        /// </summary>
        Task DeleteAsync(T entity);
    }
}
