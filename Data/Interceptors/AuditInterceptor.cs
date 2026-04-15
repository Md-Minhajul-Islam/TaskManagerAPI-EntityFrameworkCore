using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data.Interceptors;

// Interceptors run BEFORE or AFTER EF Core operations
// SaveChangesInterceptor runs around every SaveChangesAsync call
// We use it to auto-set shadow properties (CreatedBy, LastLoginAt)
public class AuditInterceptor : SaveChangesInterceptor
{
    // SavingChangesAsync runs Before changes are saved to DB
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if(eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var context = eventData.Context;

        foreach(var entry in context.ChangeTracker.Entries<User>())
        {
            if(entry.State == EntityState.Added)
            {
                // Set shadow property CreatedBy when a new User is created
                // In a real app, you'd get this from IHttpContextAccessor
                entry.Property("CreatedBy").CurrentValue = "System";
            }

            
            if (entry.State == EntityState.Added ||
                entry.State == EntityState.Modified)
            {
                // We could set LastLoginAt here if needed
                // For now, just demonstrate the shadow property exists
                // entry.Property("LastLoginAt").CurrentValue = DateTime.UtcNow;
            }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}