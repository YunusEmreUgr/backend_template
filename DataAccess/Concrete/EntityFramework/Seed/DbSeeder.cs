using Core.Entities.Concrete.Users;
using Core.Utilities.Security.Hashing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework.Seed
{
    /// <summary>
    /// Veritabanı başlangıç verilerini (seed data) asenkron olarak dolduran ve pending
    /// migrations'ları uygulayan yardımcı sınıf.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // 1. Pending Migrations'ları otomatik uygula (eğer EF Migrations kullanılıyorsa)
            try
            {
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    await context.Database.MigrateAsync();
                }
            }
            catch
            {
                // SQLite EnsureCreated fallback
            }

            // 2. Varsayılan Yetkileri (Operation Claims) Seed Et
            var defaultClaims = new[] { "Admin", "Moderator", "User", "Premium" };
            foreach (var claimName in defaultClaims)
            {
                if (!await context.OperationClaims.AnyAsync(oc => oc.OperationClaimName == claimName))
                {
                    await context.OperationClaims.AddAsync(new OperationClaim
                    {
                        OperationClaimName = claimName
                    });
                }
            }
            await context.SaveChangesAsync();

            // 3. Varsayılan Admin Kullanıcısını Seed Et
            var adminEmail = "admin@template.com";
            var adminClaim = await context.OperationClaims.FirstOrDefaultAsync(oc => oc.OperationClaimName == "Admin");

            if (adminClaim != null)
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (adminUser == null)
                {
                    HashingHelper.CreatePasswordHash("Admin123!", out byte[] passwordHash, out byte[] passwordSalt);

                    adminUser = new User
                    {
                        FirstName = "System",
                        LastName = "Administrator",
                        Email = adminEmail,
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Status = true,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await context.Users.AddAsync(adminUser);
                    await context.SaveChangesAsync();
                }

                var hasAdminClaimRelation = await context.UserOperationClaims
                    .AnyAsync(uoc => uoc.UserId == adminUser.UserId && uoc.OperationClaimId == adminClaim.OperationClaimId);

                if (!hasAdminClaimRelation)
                {
                    await context.UserOperationClaims.AddAsync(new UserOperationClaim
                    {
                        UserId = adminUser.UserId,
                        OperationClaimId = adminClaim.OperationClaimId
                    });
                    await context.SaveChangesAsync();
                }
            }

            // 4. Varsayılan Örnek Ürünleri Seed Et (Eğer veritabanı boşsa)
            if (!await context.Products.AnyAsync())
            {
                await context.Products.AddRangeAsync(new[]
                {
                    new Entities.Concrete.Product
                    {
                        Name = "MacBook Pro 16 M3 Max",
                        Price = 129999.00m,
                        Stock = 12,
                        Description = "Kurumsal performans dizüstü bilgisayar.",
                        CategoryId = 1,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    },
                    new Entities.Concrete.Product
                    {
                        Name = "Enterprise Cloud Server",
                        Price = 249999.00m,
                        Stock = 5,
                        Description = "Yüksek erişilebilirlikli sunucu altyapısı.",
                        CategoryId = 2,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    }
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
