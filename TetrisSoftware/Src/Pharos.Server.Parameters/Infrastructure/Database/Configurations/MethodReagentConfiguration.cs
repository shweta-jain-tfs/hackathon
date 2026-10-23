using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class MethodReagentConfiguration : BaseAuditableEntityConfiguration<MethodReagent>
{
    public override void Configure(EntityTypeBuilder<MethodReagent> builder)
    {
        base.Configure(builder);

        // Foreign keys
        builder.HasOne(x => x.Method)
               .WithMany(y => y.Reagents)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => new
        {
            x.MethodId,
            x.ReagentType
        }).IsUnique();
    }
}