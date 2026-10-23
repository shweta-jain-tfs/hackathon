using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;

internal sealed class TestReplicateConfiguration : BaseEntityConfiguration<TestReplicate>
{
    public override void Configure(EntityTypeBuilder<TestReplicate> builder)
    {
        base.Configure(builder);

        builder.HasDiscriminator<TestResultType>(nameof(TestResult.ResultType))
            .HasValue<SampleTestReplicate>(TestResultType.Sample)
            .HasValue<CalibratorTestReplicate>(TestResultType.Calibrator)
            .HasValue<QualityControlTestReplicate>(TestResultType.QualityControl)
            .HasValue<CurveControlTestReplicate>(TestResultType.CurveControl);

        builder.Property(x => x.InstrumentReplicateId)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(x => x.InstrumentReplicateId).IsUnique();
        builder.HasIndex(x => x.TestResultId);
        builder.HasIndex(x => x.Response); // to quickly find replicates without response after receiving a blank result
    }
}