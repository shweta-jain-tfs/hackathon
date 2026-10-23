using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodCalibrationCurveConfiguration : BaseAuditableEntityConfiguration<MethodCalibrationCurve>
{
    public override void Configure(EntityTypeBuilder<MethodCalibrationCurve> builder)
    {
        base.Configure(builder);

        // Foreign keys
        builder.HasOne(x => x.Method)
               .WithOne(y => y.CalibrationCurve)
               .HasForeignKey<MethodCalibrationCurve>(x => x.MethodId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MethodTest)
               .WithMany(y => y.CalibrationCurves)
               .HasForeignKey(x => x.MethodTestId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}