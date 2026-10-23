using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;

internal sealed class CurrentCurveConfiguration : BaseAuditableEntityConfiguration<CurrentCurve>
{
    public override void Configure(EntityTypeBuilder<CurrentCurve> builder)
    {
        base.Configure(builder);

        builder.HasOne(x => x.Curve).WithMany(y => y.CurrentCurves)
            .OnDelete(DeleteBehavior.Cascade);
    }
}