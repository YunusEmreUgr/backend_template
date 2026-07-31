using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Design-time EF Core Migrations işlemleri için DbContext Factory sınıfı.
    /// terminalde "dotnet ef migrations add" komutunun çalışmasını sağlar.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Burası yerel veya geliştirme veritabanı bağlantı dizesidir.
            // Migrations işlemleri için şablon olarak bırakılmıştır.
            optionsBuilder.UseNpgsql("Host=localhost;Database=CleanArchTemplateDb;Username=postgres;Password=your_password;Port=5432;");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
