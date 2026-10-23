using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Infrastructure.Database.Configurations;

internal sealed class QueuedLisMessageConfiguration : BaseEntityConfiguration<QueuedLisMessage>
{
    public override void Configure(EntityTypeBuilder<QueuedLisMessage> builder)
    {
        base.Configure(builder);

        builder.HasIndex(m => new { m.Priority, m.Created }).IsDescending(true, false);
    }
}

