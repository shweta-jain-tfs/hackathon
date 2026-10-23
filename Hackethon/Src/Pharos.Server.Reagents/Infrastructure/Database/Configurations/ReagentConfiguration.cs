using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Reagents.Infrastructure.Database.Configurations;

internal sealed class ReagentConfiguration : BaseEntityConfiguration<Reagent>
{
    public override void Configure(EntityTypeBuilder<Reagent> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.Barcode);
        builder.HasIndex(x => x.Udi);

        builder.HasIndex(x => x.LotId);

        builder.HasOne(x => x.Lot)
            .WithMany(x => x.Reagents);
    }
}