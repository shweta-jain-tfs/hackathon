using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodConcentrationUnitConfiguration : IEntityTypeConfiguration<MethodConcentrationUnit>
{
    public void Configure(EntityTypeBuilder<MethodConcentrationUnit> builder)
    {
        builder.HasKey(x => new { x.MethodId, x.UnitId });

        // Foreign keys
        builder.HasOne(x => x.Method)
               .WithMany(y => y.ConcentrationUnits)
               .OnDelete(DeleteBehavior.Cascade);
    }
}