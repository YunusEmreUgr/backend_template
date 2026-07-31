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

    }
}
