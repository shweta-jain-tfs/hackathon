using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Infrastructure.Database.Configurations;

internal sealed class SampleTrayConfiguration : IEntityTypeConfiguration<SampleTray>
{
    public void Configure(EntityTypeBuilder<SampleTray> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.SampleTubes)
            .WithOne()
            .HasForeignKey(x => x.TrayId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
