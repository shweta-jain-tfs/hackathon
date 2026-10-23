using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Infrastructure.Database;

internal class SystemUpdateDbContext : BaseDbContext<SystemUpdateDbContext>, ISystemUpdateDbContext
{
    public const string Schema = "SystemUpdate";

    public SystemUpdateDbContext(IConfiguration configuration, IAuditableEntitySaveChangesInterceptor? auditableEntitySaveChangesInterceptor, IMediator? mediator)
        : base(configuration, auditableEntitySaveChangesInterceptor, mediator)
    {
    }

    protected override string DefaultSchema => Schema;

    public DbSet<SystemUpdateSession> SystemUpdateSessions => Set<SystemUpdateSession>();

    public Task<SystemUpdateSession?> GetLatestPendingAsync(CancellationToken cancellationToken = default)
    {
        return SystemUpdateSessions
            .Where(uc => uc.Status == UpdateStatus.Pending)
            .OrderByDescending(uc => uc.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<SystemUpdateSession?> GetLatestUnacknowledgedAsync(CancellationToken cancellationToken = default)
    {
        return SystemUpdateSessions
            .Where(s => s.AcknowledgedAtUtc == null)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
