using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;

internal sealed class BlankReplicateConfiguration : BaseEntityConfiguration<BlankReplicate>
{
    public override void Configure(EntityTypeBuilder<BlankReplicate> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.BlankId)
            .IsRequired();

        builder.Property(x => x.MeasurementTime)
            .IsRequired();

        // Foreign keys
        builder.HasOne<Blank>()
            .WithMany(y => y.BlankReplicates)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.BlankId);
        builder.HasIndex(x => x.MeasurementTime);
    }
}
