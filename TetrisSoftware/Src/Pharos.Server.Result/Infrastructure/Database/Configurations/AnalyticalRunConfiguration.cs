using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;

internal sealed class AnalyticalRunConfiguration : BaseEntityConfiguration<AnalyticalRun>
{
    public override void Configure(EntityTypeBuilder<AnalyticalRun> builder)
    {
        base.Configure(builder);

        builder.HasOne(x => x.CalculationCurve)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.InstrumentNumber, x.InstrumentAnalyticalRunId })
            .IsUnique();

        builder.Property(x => x.CurveSource)
            .HasDefaultValue(CurveSource.Missing);

        builder.Property(x => x.CurveState)
            .HasDefaultValue(CurveState.Valid);
    }
}
