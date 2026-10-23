using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;

namespace Pharos.Server.Maintenance.Infrastructure.Database;

internal class MaintenanceDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<MaintenanceDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), IMaintenanceDbContext
{
    public const string Schema = "Maintenance";

    protected override string DefaultSchema => Schema;

    public DbSet<MaintenanceActivity> Activities => Set<MaintenanceActivity>();
}