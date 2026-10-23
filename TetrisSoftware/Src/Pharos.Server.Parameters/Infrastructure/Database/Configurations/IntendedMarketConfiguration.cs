using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class IntendedMarketConfiguration : IEntityTypeConfiguration<IntendedMarket>
{
    public void Configure(EntityTypeBuilder<IntendedMarket> builder)
    {
        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired();
    }
}