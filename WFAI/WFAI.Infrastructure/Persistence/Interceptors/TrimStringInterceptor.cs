using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WFAI.Infrastructure.Persistence.Interceptors
{
    public class TrimStringInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            TrimStrings(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            TrimStrings(eventData.Context);
            return ValueTask.FromResult(result);
        }

        private void TrimStrings(DbContext? context)
        {
            if (context == null)
                return;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    foreach (PropertyEntry prop in entry.Properties)
                    {
                        if (prop.Metadata.ClrType == typeof(string) &&
                            prop.CurrentValue is string value)
                        {
                            var trimmed = value.Trim();

                            if (trimmed != value)
                            {
                                prop.CurrentValue = trimmed;
                            }
                        }
                    }
                }
            }
        }
    }
}