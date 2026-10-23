using Pharos.Server.Reflex.Domain;

namespace Pharos.Server.Reflex.Infrastructure.Database.Configurations;

internal sealed class ReflexDefinitionConfiguration : BaseAuditableEntityConfiguration<ReflexDefinition>
{
    public override void Configure(EntityTypeBuilder<ReflexDefinition> builder)
    {
        base.Configure(builder);

        builder.OwnsOne(x => x.ReflexCondition);

        builder.HasMany(x => x.ReflexTests)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}