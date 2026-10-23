using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.TaskCard.Infrastructure.Database;

internal class TaskCardDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<TaskCardDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), ITaskCardDbContext
{
    public const string Schema = "TaskCard";

    protected override string DefaultSchema => Schema;

    public DbSet<Domain.TaskCard> TaskCards => Set<Domain.TaskCard>();
}