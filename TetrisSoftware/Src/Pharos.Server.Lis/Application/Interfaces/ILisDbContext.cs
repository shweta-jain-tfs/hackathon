using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Interfaces;

internal interface ILisDbContext : IDbContext
{
    public DbSet<QueuedLisMessage> QueuedLisMessages { get; }
    public DbSet<LisStatistics> LisStatistics { get; }

    EntityEntry Entry(object entity);
}