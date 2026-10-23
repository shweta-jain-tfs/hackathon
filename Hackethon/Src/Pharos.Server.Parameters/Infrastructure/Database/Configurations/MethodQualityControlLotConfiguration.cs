using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodQualityControlLotConfiguration : BaseAuditableEntityConfiguration<MethodQualityControlLot>
{
    public override void Configure(EntityTypeBuilder<MethodQualityControlLot> builder)
    {
        // Columns
        builder.Property(x => x.LotNumber)
               .IsRequired();
        builder.Property(x => x.LotType).IsRequired();

        // Foreign keys
        builder.HasOne(x => x.QualityControlTest)
              .WithMany(y => y.Lots)
              .HasForeignKey(x => x.MethodQualityControlId)
              .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.LotNumber);

        base.Configure(builder);
    }
}