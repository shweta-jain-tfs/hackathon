using Pharos.Server.Domain.Parameters;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class PanelConfiguration : BaseAuditableEntityConfiguration<Panel>
{
    public override void Configure(EntityTypeBuilder<Panel> builder)
    {
        base.Configure(builder);

        builder.Property(p => p.PanelName)
            .IsRequired();

        builder.HasIndex(p => p.PanelName)
            .IsUnique();
    }
}
