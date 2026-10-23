using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class QualityControlSchedulingConfiguration : BaseEntityConfiguration<QualityControlSchedule>
{
    public override void Configure(EntityTypeBuilder<QualityControlSchedule> builder)
    {
        base.Configure(builder);

        builder.HasOne(x => x.QualityControlTest)
            .WithOne(x => x.Schedule)
            .HasForeignKey<QualityControlSchedule>(x => x.QualityControlTestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}