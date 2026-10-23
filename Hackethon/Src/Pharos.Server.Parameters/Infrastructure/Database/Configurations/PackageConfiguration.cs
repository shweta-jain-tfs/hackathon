using Pharos.Server.Domain.Parameters;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class PackageConfiguration : BaseAuditableEntityConfiguration<Package>
{
    public override void Configure(EntityTypeBuilder<Package> builder)
    {
        // Columns
        builder.Property(x => x.PackageBarcode)
               .IsRequired();
        builder.Property(x => x.OrderingNumber)
               .IsRequired();

        builder.Property(x => x.IntendedMarketId)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.PackageBarcode)
            .IsUnique();
        builder.HasIndex(x => x.Hidden);

        builder.HasIndex(x => x.IntendedMarketId);

        builder.HasOne(x => x.IntendedMarket)
            .WithMany(x => x.Packages);

        base.Configure(builder);
    }
}