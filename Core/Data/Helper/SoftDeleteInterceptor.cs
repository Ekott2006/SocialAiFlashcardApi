using Core.Model.Helper;
using Core.Model.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Core.Data.Helper;

public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        IEnumerable<EntityEntry<ISoftDelete>> entries = eventData.Context?.ChangeTracker.Entries<ISoftDelete>()
            .Where(e => e.State == EntityState.Deleted) ?? [];

        foreach (EntityEntry<ISoftDelete> entry in entries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
        }
        return base.SavingChangesAsync(eventData, result, ct);
    }
}