using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodClassTableConfiguration : BaseAuditableEntityConfiguration<MethodClassTable>
{
    public override void Configure(EntityTypeBuilder<MethodClassTable> builder)
    {
        // Foreign keys
        builder.HasOne(x => x.Method)
               .WithMany(y => y.ClassTables)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => new
        {
            x.MethodId,
            x.ClassTableId,
            x.ClassTableRowId
        })
               .IsUnique();

        base.Configure(builder);
    }
}