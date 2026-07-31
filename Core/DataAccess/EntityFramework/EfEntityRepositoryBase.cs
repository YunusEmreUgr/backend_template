using Core.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Core.DataAccess.EntityFramework
{
    /// <summary>
    /// Entity Framework Core kullanarak IEntityRepository'yi implement eden generic base sınıf.
    /// 
    /// Tüm repository implementasyonları bu sınıftan miras alır:
    ///   public class EfProductDal : EfEntityRepositoryBase&lt;Product, AppDbContext&gt;, IProductDal { }
    /// 
    /// Özellikler:
    /// ─ AsNoTracking() ile performans optimizasyonu (okuma işlemlerinde %80-90 daha hızlı)
    /// ─ Soft delete desteği (IsDeleted alanı varsa silinmiş olarak işaretler)
    /// ─ Sayfalama desteği (GetPagedAsync)
    /// ─ Include/navigation property desteği
    /// ─ Tracking çakışması önleme (DetachLocalIfTracking)
    /// ─ ConcurrentDictionary ile cached reflection (performans)
    /// 
    /// Generic parametreler:
    ///   TEntity → Yönetilecek entity türü
    ///   TContext → Entity'nin DbContext'i
    /// </summary>
    public class EfEntityRepositoryBase<TEntity, TContext> : IEntityRepository<TEntity>
        where TEntity : class, IEntity, new()
        where TContext : DbContext
    {
        protected TContext Context { get; }

        // ⚡ PERFORMANS: Reflection sonuçları cache'lenir - her çağrıda reflection çalışmaz
        private static readonly ConcurrentDictionary<Type, PropertyInfo?> _createdAtPropertyCache
            = new ConcurrentDictionary<Type, PropertyInfo?>();

        private static readonly ConcurrentDictionary<Type, PropertyInfo?> _idPropertyCache
            = new ConcurrentDictionary<Type, PropertyInfo?>();

        public EfEntityRepositoryBase(TContext context)
        {
            Context = context;
        }

        // ─── Okuma Metodları ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<List<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = Context.Set<TEntity>();

            // Navigation property'leri eager load et
            foreach (var include in includes)
                query = query.Include(include);

            if (filter != null)
                query = query.Where(filter);

            // ⚡ AsNoTracking: Change tracking kapalı → bellek tasarrufu, hız artışı
            return await query.AsNoTracking().ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter)
        {
            return await Context.Set<TEntity>().AsNoTracking().SingleOrDefaultAsync(filter);
        }

        /// <inheritdoc/>
        public async Task<TEntity?> GetByIdAsync(int id)
        {
            // FindAsync, primary key cache'ini kullanır → tek kayıt için en hızlı yol
            var entity = await Context.Set<TEntity>().FindAsync(id);
            if (entity != null)
            {
                // Sonraki işlemlerde tracking çakışmasını önlemek için detach et
                Context.Entry(entity).State = EntityState.Detached;
            }
            return entity;
        }

        /// <inheritdoc/>
        public async Task<TEntity?> GetWithIncludesAsync(
            Expression<Func<TEntity, bool>> filter,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = Context.Set<TEntity>();

            foreach (var include in includes)
                query = query.Include(include);

            return await query.AsNoTracking().SingleOrDefaultAsync(filter);
        }

        /// <inheritdoc/>
        public async Task<List<TEntity>> GetAllWithIncludesAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = Context.Set<TEntity>();

            foreach (var include in includes)
                query = query.Include(include);

            if (filter != null)
                query = query.Where(filter);

            return await query.AsNoTracking().ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            int pageNumber = 1,
            int pageSize = 10,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = Context.Set<TEntity>();

            foreach (var include in includes)
                query = query.Include(include);

            if (filter != null)
                query = query.Where(filter);

            // ⚡ PERFORMANS: Cached reflection - her çağrıda GetProperty() yapma
            // CreatedAt varsa → en yeni içerik önce gelir
            var createdAtProperty = _createdAtPropertyCache.GetOrAdd(
                typeof(TEntity),
                t => t.GetProperty("CreatedAt")
            );

            if (createdAtProperty != null)
            {
                query = query.OrderByDescending(e => EF.Property<DateTime>(e, "CreatedAt"));
            }
            else
            {
                // CreatedAt yoksa primary key'e göre sırala
                var idProperty = _idPropertyCache.GetOrAdd(
                    typeof(TEntity),
                    t => t.GetProperties()
                        .FirstOrDefault(p => p.Name.EndsWith("Id") && p.PropertyType == typeof(int))
                );

                if (idProperty != null)
                {
                    query = query.OrderByDescending(e => EF.Property<int>(e, idProperty.Name));
                }
            }

            return await query
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // ─── Sayım ve Kontrol Metodları ───────────────────────────────────────

        /// <inheritdoc/>
        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null)
        {
            return filter == null
                ? await Context.Set<TEntity>().CountAsync()
                : await Context.Set<TEntity>().CountAsync(filter);
        }

        /// <inheritdoc/>
        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter)
        {
            return await Context.Set<TEntity>().AnyAsync(filter);
        }

        // ─── Yazma Metodları ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task AddAsync(TEntity entity)
        {
            await Context.Set<TEntity>().AddAsync(entity);
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(TEntity entity)
        {
            // Tracking çakışmasını önle
            DetachLocalIfTracking(entity);
            Context.Set<TEntity>().Update(entity);
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(TEntity entity)
        {
            // Soft delete: entity'de IsDeleted alanı varsa sil bayrağı koy
            var isDeletedProperty = entity.GetType().GetProperty("IsDeleted");
            if (isDeletedProperty != null)
            {
                // Hem IsDeleted hem de IsActive güncelle (data consistency)
                isDeletedProperty.SetValue(entity, true);

                var isActiveProperty = entity.GetType().GetProperty("IsActive");
                if (isActiveProperty != null)
                    isActiveProperty.SetValue(entity, false);

                // DeletedAt timestamp'i set et (audit için)
                var deletedAtProperty = entity.GetType().GetProperty("DeletedAt");
                if (deletedAtProperty != null)
                    deletedAtProperty.SetValue(entity, DateTime.UtcNow);

                Context.Set<TEntity>().Update(entity);
            }
            else
            {
                // Hard delete: entity'de IsDeleted yoksa fiziksel olarak sil
                DetachLocalIfTracking(entity);
                Context.Set<TEntity>().Remove(entity);
            }

            await Context.SaveChangesAsync();
        }

        // ─── Private Yardımcılar ─────────────────────────────────────────────

        /// <summary>
        /// Aynı PK'ya sahip farklı bir entity instance zaten tracking'deyse detach eder.
        /// Bu olmadan Update işlemi "entity with same key already tracked" hatası verir.
        /// </summary>
        private void DetachLocalIfTracking(TEntity entity)
        {
            var entityType = Context.Model.FindEntityType(typeof(TEntity));
            var primaryKey = entityType?.FindPrimaryKey();
            if (primaryKey == null) return;

            var keyValues = primaryKey.Properties
                .Select(p => p.PropertyInfo?.GetValue(entity))
                .ToArray();

            var trackedEntry = Context.ChangeTracker.Entries<TEntity>()
                .FirstOrDefault(e => primaryKey.Properties
                    .Select(p => p.PropertyInfo?.GetValue(e.Entity))
                    .SequenceEqual(keyValues));

            // Aynı kayıt farklı instance ile track ediliyorsa detach et
            if (trackedEntry != null && !ReferenceEquals(trackedEntry.Entity, entity))
            {
                trackedEntry.State = EntityState.Detached;
            }
        }
    }
}
