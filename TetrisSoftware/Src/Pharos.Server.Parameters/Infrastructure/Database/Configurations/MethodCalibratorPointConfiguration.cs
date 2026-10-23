using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodCalibratorPointConfiguration : BaseAuditableEntityConfiguration<MethodCalibrationPoint>
{
    public override void Configure(EntityTypeBuilder<MethodCalibrationPoint> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.CalibratorName)
               .IsRequired();

        // Foreign keys
        builder.HasOne(x => x.CalibrationCurve)
               .WithMany(y => y.CalibrationPoints)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => new
        {
            MethodCalibratorsId = x.MethodCalibrationCurveId,
            x.CalibratorName
        }).IsUnique();
    }
}