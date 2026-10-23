using Microsoft.Extensions.Configuration;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Infrastructure.Database;

internal class SampleDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<SampleDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), ISampleDbContext
{
    public const string Schema = "Sample";

    protected override string DefaultSchema => Schema;

    public DbSet<SampleTube> SampleTubes => Set<SampleTube>();
    public DbSet<SampleTray> SampleTrays => Set<SampleTray>();
}
