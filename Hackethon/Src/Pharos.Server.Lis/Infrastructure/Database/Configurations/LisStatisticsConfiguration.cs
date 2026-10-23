using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Infrastructure.Database.Configurations;

internal sealed class LisStatisticsConfiguration : BaseEntityConfiguration<LisStatistics>
{
    public override void Configure(EntityTypeBuilder<LisStatistics> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => new { x.ChannelId, x.LisStatisticsCategory }).IsUnique();
    }
}
