using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Infrastructure.Database.Configurations;

internal class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public virtual void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.Property(x => x.Key);
        builder.HasKey(x => x.Key);

        builder.Property(x => x.Value);
        builder.Property(x => x.ProtectedValue);

        builder.Property(x => x.LastModified)
            .HasConversion<string>();

        builder.Property(x => x.LastModifiedBy)
            .HasConversion<string>();
    }
}
