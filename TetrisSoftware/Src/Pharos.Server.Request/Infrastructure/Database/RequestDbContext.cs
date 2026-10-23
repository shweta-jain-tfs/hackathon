using MediatR;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Infrastructure.Database;

internal class RequestDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<RequestDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), IRequestDbContext
{
    public const string Schema = "Request";

    protected override string DefaultSchema => Schema;

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Domain.Request> Requests => Set<Domain.Request>();
    public DbSet<TestOrder> TestOrders => Set<TestOrder>();
    public DbSet<TestOrderCustomParameter> TestOrderCustomParameters => Set<TestOrderCustomParameter>();

    public DbSet<RequestIssue> Issues => Set<RequestIssue>();

    public DbSet<Requestor> Requestors => Set<Requestor>();
    
    public DbSet<ReflexDefinition> ReflexDefinitions => Set<ReflexDefinition>();
    public DbSet<ReflexTest> ReflexTests => Set<ReflexTest>();

    public void ClearChanges()
    {
        ChangeTracker.Clear();
    }
}