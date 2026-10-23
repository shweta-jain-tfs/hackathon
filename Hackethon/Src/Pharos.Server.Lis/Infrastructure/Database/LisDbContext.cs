using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Infrastructure.Database;

internal class LisDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<LisDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), ILisDbContext
{
    public const string Schema = "Lis";

    protected override string DefaultSchema => Schema;

    public DbSet<QueuedLisMessage> QueuedLisMessages => Set<QueuedLisMessage>();
    public DbSet<LisStatistics> LisStatistics => Set<LisStatistics>();
}
