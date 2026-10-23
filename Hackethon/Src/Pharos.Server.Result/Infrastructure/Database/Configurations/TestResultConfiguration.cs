using Pharos.Server.Domain;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;

internal sealed class TestResultConfiguration : BaseAuditableEntityConfiguration<TestResult>
{
    public const string TestOrderGroupId = "TestOrderGroupId";

    public override void Configure(EntityTypeBuilder<TestResult> builder)
    {
        base.Configure(builder);

        builder.Ignore(x => x.CreatedBy); // test results are created automatically 

        builder.HasDiscriminator<TestResultType>(nameof(TestResult.ResultType))
               .HasValue<SampleTestResult>(TestResultType.Sample)
               .HasValue<CalibratorTestResult>(TestResultType.Calibrator)
               .HasValue<CurveControlTestResult>(TestResultType.CurveControl)
               .HasValue<QualityControlTestResult>(TestResultType.QualityControl);

        builder.HasMany(x => x.Replicates)
            .WithOne(x => x.TestResult)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AnalyticalRun)
            .WithMany(x => x.TestResults)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.AnalyticalRunId, x.MeasurementDateTime });
        builder.HasIndex(x => new { x.InstrumentNumber, x.MethodName, x.MeasurementDateTime });

        // Disable SQL OUTPUT for this table because SampleIdNaturalSortKey is a computed column
        // using dbo.NaturalSortKey(...), which SQL Server cannot project in OUTPUT INSERTED.
        builder.ToTable(t => t.UseSqlOutputClause(false));
    }
}

internal sealed class SampleTestResultConfiguration : IEntityTypeConfiguration<SampleTestResult>
{
    public void Configure(EntityTypeBuilder<SampleTestResult> builder)
    {
        builder.OwnsOne(x => x.RequestUid, xx => xx.HasIndex(o => new
        {
            o.SampleId,
            RequestDate = o.RequestCreatedDate
        }));

        builder.Property(x => x.SampleIdNaturalSortKey)
            .HasComputedColumnSql($"dbo.NaturalSortKey({nameof(SampleTestResult.RequestUid)}_{nameof(RequestUid.SampleId)})");

        builder.Property(x => x.TestNameNaturalSortKey)
            .HasComputedColumnSql($"dbo.NaturalSortKey({nameof(SampleTestResult.TestName)})");

        builder.Property(x => x.MeanClassNaturalSortKey)
            .HasComputedColumnSql($"dbo.NaturalSortKey({nameof(SampleTestResult.MeanClass)})");

        builder.Property(x => x.MeanCutOffNaturalSortKey)
            .HasComputedColumnSql($"dbo.NaturalSortKey({nameof(SampleTestResult.MeanCutOff)})");

        builder.Property(x => x.MeanQuotientNaturalSortKey)
            .HasComputedColumnSql($"dbo.NaturalSortKey({nameof(SampleTestResult.MeanQuotientAsText)})");

        builder.Property(x => x.TestOrderId)
            .HasColumnName(TestResultConfiguration.TestOrderGroupId);

        builder.Property(x => x.ApprovalStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(nameof(SampleTestResult.ApprovalStatus), nameof(TestResult.Created));
        builder.HasIndex(nameof(TestResult.ResultType), nameof(SampleTestResult.ApprovalStatus), nameof(TestResult.Created));

        builder.HasIndex(x => x.TestOrderId);
    }
}

internal sealed class CalibratorTestResultConfiguration : IEntityTypeConfiguration<CalibratorTestResult>
{
    public void Configure(EntityTypeBuilder<CalibratorTestResult> builder)
    {
        builder.Property(x => x.GroupId)
            .HasColumnName(TestResultConfiguration.TestOrderGroupId);

        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.CurveId);
    }
}

internal sealed class CurveControlTestResultConfiguration : IEntityTypeConfiguration<CurveControlTestResult>
{
    public void Configure(EntityTypeBuilder<CurveControlTestResult> builder)
    {
        builder.Property(x => x.GroupId)
            .HasColumnName(TestResultConfiguration.TestOrderGroupId);

        builder.HasIndex(x => x.GroupId);
    }
}

internal sealed class QualityControlTestResultConfiguration : IEntityTypeConfiguration<QualityControlTestResult>
{
    public void Configure(EntityTypeBuilder<QualityControlTestResult> builder)
    {
        builder.Property(x => x.GroupId)
            .HasColumnName(TestResultConfiguration.TestOrderGroupId);

        builder.HasIndex(x => x.GroupId);

        builder.Property(x => x.TubeId)
            .HasMaxLength(Constants.TubeIdMaxLength);
    }
}