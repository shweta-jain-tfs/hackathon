using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Interfaces;

internal interface ISystemUpdateDbContext : IDbContext
{
    DbSet<SystemUpdateSession> SystemUpdateSessions { get; }

    Task<SystemUpdateSession?> GetLatestPendingAsync(CancellationToken cancellationToken = default);
    Task<SystemUpdateSession?> GetLatestUnacknowledgedAsync(CancellationToken cancellationToken = default);
}