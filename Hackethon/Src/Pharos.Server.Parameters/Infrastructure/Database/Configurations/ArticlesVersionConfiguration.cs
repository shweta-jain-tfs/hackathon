using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class ArticlesVersionConfiguration : IEntityTypeConfiguration<ArticlesVersion>
{
    public void Configure(EntityTypeBuilder<ArticlesVersion> builder)
    {
        builder.Property(x => x.VersionNumber).ValueGeneratedNever();

        builder.HasKey(x => x.VersionNumber);
    }
}