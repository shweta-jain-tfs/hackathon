using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Infrastructure.Database.Configurations;

internal sealed class CurveConfiguration : BaseEntityConfiguration<Curve>
{
    public override void Configure(EntityTypeBuilder<Curve> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => new { x.InstrumentNumber, x.GroupId })
            .IsUnique();

        builder.HasMany(x => x.TestResults)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}