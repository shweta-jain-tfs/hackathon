using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodCurveControlConfiguration : IEntityTypeConfiguration<MethodCurveControl>
{
    public void Configure(EntityTypeBuilder<MethodCurveControl> builder)
    {
        builder.HasKey(x => new { x.MethodId, x.CurveControlName });

        // Foreign keys
        builder.HasOne(x => x.Method)
               .WithMany(y => y.CurveControls)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MethodTest)
               .WithMany(y => y.CurveControls)
               .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(x => new
        {
            x.MethodId,
            x.CurveControlName
        })
               .IsUnique();
    }
}