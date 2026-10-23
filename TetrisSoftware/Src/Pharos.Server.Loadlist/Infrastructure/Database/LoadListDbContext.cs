using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Infrastructure.Database;

internal class LoadListDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor? auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<LoadListDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), ILoadListDbContext
{
    public const string Schema = "LoadList";
    protected override string DefaultSchema => Schema;

    public DbSet<LoadListConfiguration> LoadListConfigurations => Set<LoadListConfiguration>();
    public DbSet<LastLoadedReagent> LastLoadedReagents => Set<LastLoadedReagent>();
}