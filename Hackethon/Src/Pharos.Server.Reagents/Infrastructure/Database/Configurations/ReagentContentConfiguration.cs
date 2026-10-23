using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Reagents.Infrastructure.Database.Configurations;

internal sealed class ReagentContentConfiguration : BaseEntityConfiguration<ReagentContent>
{
    public override void Configure(EntityTypeBuilder<ReagentContent> builder)
    {
        base.Configure(builder);

        builder.HasOne<Reagent>()
            .WithMany(x => x.ReagentContents);
    }
}