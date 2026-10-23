using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class ParameterUpdatePackageVersionConfiguration : IEntityTypeConfiguration<ParameterUpdatePackageVersion>
{
    public void Configure(EntityTypeBuilder<ParameterUpdatePackageVersion> builder)
    {
        builder.Property(x => x.VersionNumber).ValueGeneratedNever();
        builder.HasKey(x => x.VersionNumber);
    }
}