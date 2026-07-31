using DataAccess.Concrete.EntityFramework;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Health
{
    /// <summary>
    /// Veritabanı bağlantısının aktif ve erişilebilir olup olmadığını kontrol eden sağlık kontrolü sınıfı.
    /// </summary>
    public class DbContextHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _context;

        public DbContextHealthCheck(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Veritabanına gerçekten bağlantı sağlanıp sağlanamadığını kontrol eder
                if (await _context.Database.CanConnectAsync(cancellationToken))
                {
                    return HealthCheckResult.Healthy("Veritabanı bağlantısı başarılı.");
                }

                return HealthCheckResult.Unhealthy("Veritabanına bağlanılamadı.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Veritabanı bağlantısı sırasında bir hata oluştu.", ex);
            }
        }
    }
}
