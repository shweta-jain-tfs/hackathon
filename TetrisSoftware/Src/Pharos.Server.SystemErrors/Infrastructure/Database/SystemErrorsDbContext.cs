using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Infrastructure.Database;

internal class SystemErrorsDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor? auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<SystemErrorsDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), ISystemErrorsDbContext
{
    public const string Schema = "Error";

    protected override string DefaultSchema => Schema;

    public DbSet<SystemError> SystemErrors => Set<SystemError>();
}