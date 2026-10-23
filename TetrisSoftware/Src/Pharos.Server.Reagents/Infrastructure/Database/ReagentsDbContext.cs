using MediatR;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Reagents.Infrastructure.Database;

internal class ReagentsDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<ReagentsDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), IReagentsDbContext
{
    public const string Schema = "Reagents";

    protected override string DefaultSchema => Schema;

    public DbSet<Reagent> Reagents => Set<Reagent>();
    public DbSet<ReagentContent> ReagentContents => Set<ReagentContent>();
    public DbSet<Lot> Lots => Set<Lot>();
}
