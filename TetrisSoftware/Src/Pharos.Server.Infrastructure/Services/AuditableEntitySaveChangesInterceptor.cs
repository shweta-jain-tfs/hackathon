using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pharos.Server.Application;
using Pharos.Server.Domain.Common;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Infrastructure.Services;

internal sealed class AuditableEntitySaveChangesInterceptor(
    ICurrentUserService currentUserService,
    IDateTime dateTime) : SaveChangesInterceptor, IAuditableEntitySaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity is IAuditCreationEntity ace)
            {
                ace.CreatedBy = currentUserService.UserId;
                if (ace.Created == DateTimeOffset.MinValue) // the timestamp could be elsewhere manually
                {
                    ace.Created = dateTime.Now;
                }
            }

            if ((entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted || entry.HasChangedOwnedEntities())
                && entry.Entity is IAuditModificationsEntity ame)
            {
                ame.LastModifiedBy = currentUserService.UserId;
                ame.LastModified = dateTime.Now;
            }
        }
    }
}

internal static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry)
    {
        return entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
    }
}
