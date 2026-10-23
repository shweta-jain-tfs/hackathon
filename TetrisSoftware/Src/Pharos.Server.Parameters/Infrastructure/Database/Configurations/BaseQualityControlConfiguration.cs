using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class BaseQualityControlConfiguration : BaseAuditableEntityConfiguration<BaseQualityControl>
{
    public override void Configure(EntityTypeBuilder<BaseQualityControl> builder)
    {
        builder.ToTable("QualityControls", ParametersDbContext.Schema);

        builder.HasDiscriminator<QualityControlSystemType>(nameof(BaseQualityControl.SystemType))
            .HasValue<LabQualityControl>(QualityControlSystemType.Lab)
            .HasValue<PhadiaQualityControl>(QualityControlSystemType.Phadia);

        builder.Property(x => x.Name)
            .IsRequired();

        builder.HasIndex(x => x.Name);

        base.Configure(builder);
    }
}
