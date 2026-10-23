using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Infrastructure.Database.Configurations;

internal sealed class ReflexDefinitionConfiguration : BaseEntityConfiguration<ReflexDefinition>
{
    public override void Configure(EntityTypeBuilder<ReflexDefinition> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.TestOrderId)
            .IsUnique(false);

        builder.OwnsOne(x => x.ReflexCondition);

        builder.HasMany(x => x.ReflexTests)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}