using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodClassTableBelowConfiguration : BaseAuditableEntityConfiguration<MethodClassTableBelow>
{
    public override void Configure(EntityTypeBuilder<MethodClassTableBelow> builder)
    {
        // Foreign keys
        builder.HasOne(x => x.Method)
               .WithMany(y => y.ClassTableBelows)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => new
        {
            x.MethodId,
            x.ClassTableId
        })
               .IsUnique();

        base.Configure(builder);
    }
}