using Pharos.Server.Domain.Parameters;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class PackageItemConfiguration : BaseAuditableEntityConfiguration<PackageItem>
{
    public override void Configure(EntityTypeBuilder<PackageItem> builder)
    {
        // Foreign keys
        builder.HasOne(x => x.Package)
               .WithMany(y => y.PackageItems)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Article)
               .WithMany().HasForeignKey(x => x.ArticleId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => new { x.PackageId, x.ArticleId })
               .IsUnique();

        builder.HasIndex(x => x.ArticleId);

        base.Configure(builder);
    }
}