using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.TaskCard.Domain;

namespace Pharos.Server.TaskCard.Infrastructure.Database.Configurations;

internal class TriggerStateConfiguration : IEntityTypeConfiguration<TriggerState>
{
    public void Configure(EntityTypeBuilder<TriggerState> builder)
    {
        builder.HasKey(x => x.Id);
    }
}