using Microsoft.Extensions.Configuration;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Infrastructure.Database;

internal class ReflexDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<ReflexDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), IReflexDbContext
{
    public const string Schema = "Reflex";

    protected override string DefaultSchema => Schema;

    public DbSet<ReflexDefinition> ReflexDefinitions => Set<ReflexDefinition>();
    public DbSet<ReflexTest> ReflexTests => Set<ReflexTest>();

}