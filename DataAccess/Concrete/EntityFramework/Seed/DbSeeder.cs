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
            // 1. Pending Migrations'ları otomatik uygula
            // Not: Eğer veritabanı henüz oluşturulmamışsa, EF Core Npgsql bunu da otomatik halleder.
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
            }

            // 2. Varsayılan Yetkileri (Operation Claims) Seed Et
            var defaultClaims = new[] { "Admin", "Moderator", "User" };
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

            // 3. Varsayılan Admin Kullanıcısını Seed Et (Eğer veritabanında hiç admin yoksa)
            var adminEmail = "admin@template.com";
            
            // "Admin" rolünü bul
            var adminClaim = await context.OperationClaims
                .FirstOrDefaultAsync(oc => oc.OperationClaimName == "Admin");

            if (adminClaim != null)
            {
                // Sistemde admin@template.com e-postasına sahip kullanıcı var mı?
                var adminUserExists = await context.Users.AnyAsync(u => u.Email == adminEmail);
                if (!adminUserExists)
                {
                    // Varsayılan güvenli şifre hash'ini oluştur: Admin123!
                    HashingHelper.CreatePasswordHash("Admin123!", out byte[] passwordHash, out byte[] passwordSalt);

                    var adminUser = new User
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

                    // Kullanıcıya Admin rolünü tanımla
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
            }
        }
    }
}
