using MediatR;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database;

internal class ResultDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<ResultDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), IResultDbContext
{
    public const string Schema = "Result";

    protected override string DefaultSchema => Schema;

    public DbSet<TestReplicate> TestReplicates => Set<TestReplicate>();
    public DbSet<TestResult> TestResults => Set<TestResult>();
    public DbSet<SampleTestResult> SampleTestResults => Set<SampleTestResult>();
    public DbSet<CalibratorTestResult> CalibratorTestResults => Set<CalibratorTestResult>();
    public DbSet<CurveControlTestResult> CurveControlTestResults => Set<CurveControlTestResult>();
    public DbSet<QualityControlTestResult> QualityControlTestResults => Set<QualityControlTestResult>();
    public DbSet<Curve> Curves => Set<Curve>();
    public DbSet<CurrentCurve> CurrentCurves => Set<CurrentCurve>();
    public DbSet<Blank> Blanks => Set<Blank>();
    public DbSet<BlankReplicate> BlankReplicates => Set<BlankReplicate>();
    public DbSet<BlankReplicateError> BlankReplicateErrors => Set<BlankReplicateError>();
    public DbSet<TestReplicateError> TestReplicateErrors => Set<TestReplicateError>();
    public DbSet<AnalyticalRun> AnalyticalRuns => Set<AnalyticalRun>();
    public DbSet<TestReplicateReagent> TestReplicateReagents => Set<TestReplicateReagent>();
}
