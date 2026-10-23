using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;
internal sealed class BlankConfiguration : BaseEntityConfiguration<Blank>
{
    public override void Configure(EntityTypeBuilder<Blank> builder)
    {
        // Indexes
        builder.HasIndex(x => new { x.InstrumentNumber, x.DevSolutionBarcode, x.DevSolutionCellPosition });

        base.Configure(builder);
    }
}
