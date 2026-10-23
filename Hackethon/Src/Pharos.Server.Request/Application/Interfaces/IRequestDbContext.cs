using Pharos.Server.Domain.Reflex;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Interfaces;

internal interface IRequestDbContext : IDbContext
{
    public DbSet<Patient> Patients { get; }
    public DbSet<Domain.Request> Requests { get; }
    public DbSet<TestOrder> TestOrders { get; }
    public DbSet<TestOrderCustomParameter> TestOrderCustomParameters { get; }

    public DbSet<RequestIssue> Issues { get; }

    public DbSet<Requestor> Requestors { get; }

    public DbSet<ReflexDefinition> ReflexDefinitions { get; }
    public DbSet<ReflexTest> ReflexTests { get; }

    void ClearChanges();
}