using Core.Entities.Concrete.Users;
using Core.Entities.Abstract;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Uygulamanın Entity Framework DbContext sınıfı.
    /// Veritabanı tablolarının eşleşmelerini ve global filtrelerini barındırır.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ─── DbSet Tanımlamaları ──────────────────────────────────────────────
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<OperationClaim> OperationClaims { get; set; }
        public DbSet<UserOperationClaim> UserOperationClaims { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Global İlişki Kuralı: Cascade Delete engellenir (Restrict)
            // Bu sayede veri tutarlılığı korunur, yanlışlıkla silinen bir kayıt alt ilişkileri otomatik silmez.
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // 2. Global Query Filters (Soft Delete)
            // IsDeleted alanı true olan kayıtlar EF Core select sorgularına otomatik olarak dahil edilmez.
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

            // 3. İndeks Tanımlamaları (Performans için)
            modelBuilder.Entity<Product>().HasIndex(p => p.CreatedAt);
            modelBuilder.Entity<Product>().HasIndex(p => p.CategoryId);
            modelBuilder.Entity<User>().HasIndex(u => u.GoogleId);
            modelBuilder.Entity<User>().HasIndex(u => u.AppleId);

            // 4. Veri Tipi Hassasiyetleri (Decimal Precision)
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);
        }

        /// <summary>
        /// SaveChanges öncesinde CreatedAt ve UpdatedAt alanlarını otomatik doldurur.
        /// Bu sayede yazılım katmanında her ekleme/güncelleme işleminde bu alanları elle set etmeye gerek kalmaz.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IEntity && (
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    // CreatedAt alanını UTC şimdi olarak set et
                    var createdAtProp = entry.Entity.GetType().GetProperty("CreatedAt");
                    if (createdAtProp != null)
                    {
                        createdAtProp.SetValue(entry.Entity, DateTime.UtcNow);
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    // UpdatedAt alanını UTC şimdi olarak set et
                    var updatedAtProp = entry.Entity.GetType().GetProperty("UpdatedAt");
                    if (updatedAtProp != null)
                    {
                        updatedAtProp.SetValue(entry.Entity, DateTime.UtcNow);
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
