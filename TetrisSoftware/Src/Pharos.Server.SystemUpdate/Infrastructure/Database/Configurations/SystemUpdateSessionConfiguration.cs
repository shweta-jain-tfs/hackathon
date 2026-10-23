using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Infrastructure.Database.Configurations;

internal sealed class SystemUpdateSessionConfiguration : IEntityTypeConfiguration<SystemUpdateSession>
{
    public void Configure(EntityTypeBuilder<SystemUpdateSession> builder)
    {
        builder.Property(s => s.Source)
            .HasConversion<string>();

        builder.Property(s => s.Status)
            .HasConversion<string>();
    }
}