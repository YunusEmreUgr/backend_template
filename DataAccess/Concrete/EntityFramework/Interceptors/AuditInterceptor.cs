using Core.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework.Interceptors
{
    /// <summary>
    /// EF Core SaveChanges işlemlerini keserek IAuditableEntity arayüzünü uygulayan
    /// sınıfların CreatedAt ve UpdatedAt alanlarını otomatik olarak doldurur.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, 
            InterceptionResult<int> result)
        {
            UpdateAuditFields(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, 
            InterceptionResult<int> result, 
            CancellationToken cancellationToken = default)
        {
            UpdateAuditFields(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateAuditFields(DbContext? context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries<IAuditableEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // CreatedAt alanının kazara değiştirilmesini önle
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
