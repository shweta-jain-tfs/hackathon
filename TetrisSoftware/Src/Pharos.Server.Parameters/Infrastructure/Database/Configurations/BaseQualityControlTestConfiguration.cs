using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class BaseQualityControlTestConfiguration : IEntityTypeConfiguration<BaseQualityControlTest>
{
    public void Configure(EntityTypeBuilder<BaseQualityControlTest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToTable("QualityControlTests", ParametersDbContext.Schema);

        builder.HasDiscriminator<QualityControlSystemType>(nameof(BaseQualityControlTest.SystemType))
            .HasValue<LabQualityControlTest>(QualityControlSystemType.Lab)
            .HasValue<PhadiaQualityControlTest>(QualityControlSystemType.Phadia);

        builder.HasOne(x => x.QualityControl)
            .WithMany(x => x.Tests)
            .HasForeignKey(x => x.QualityControlId);
    }
}
