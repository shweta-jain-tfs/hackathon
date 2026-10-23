using Pharos.Server.Domain.Parameters;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class PanelTestConfiguration : BaseAuditableEntityConfiguration<PanelTest>
{
    public override void Configure(EntityTypeBuilder<PanelTest> builder)
    {
        base.Configure(builder);

        builder.HasOne(pt => pt.Panel)
            .WithMany(p => p.Tests);

        builder.HasIndex(p => new { p.PanelId, p.MethodTestId })
            .IsUnique();
    }
}