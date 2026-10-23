using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Interfaces;

internal interface IResultDbContext : IDbContext
{
    public DbSet<TestReplicate> TestReplicates { get; }

    public DbSet<TestResult> TestResults { get; }
    public DbSet<SampleTestResult> SampleTestResults { get; }
    public DbSet<CalibratorTestResult> CalibratorTestResults { get; }
    public DbSet<CurveControlTestResult> CurveControlTestResults { get; }
    public DbSet<QualityControlTestResult> QualityControlTestResults { get; }

    public DbSet<Curve> Curves { get; }
    public DbSet<CurrentCurve> CurrentCurves { get; }

    public DbSet<Blank> Blanks { get; }
    public DbSet<BlankReplicate> BlankReplicates { get; }
    public DbSet<BlankReplicateError> BlankReplicateErrors { get; }
    public DbSet<TestReplicateError> TestReplicateErrors { get; }
    public DbSet<AnalyticalRun> AnalyticalRuns { get; }

    public DbSet<TestReplicateReagent> TestReplicateReagents { get; }
}