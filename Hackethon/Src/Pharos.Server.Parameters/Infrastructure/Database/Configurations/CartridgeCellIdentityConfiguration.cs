using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class CartridgeCellIdentityConfiguration : IEntityTypeConfiguration<CartridgeCellIdentity>
{
    public void Configure(EntityTypeBuilder<CartridgeCellIdentity> builder)
    {
        builder.HasKey(x => new { x.ArticleId, x.CellPosition });

        builder.HasOne(x => x.Article)
            .WithMany(x => x.CartridgeCells);
    }
}