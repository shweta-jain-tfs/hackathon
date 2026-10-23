using Pharos.Server.Domain.Parameters;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Parameters.Infrastructure.Database.Configurations;

internal sealed class ArticleConfiguration : BaseAuditableEntityConfiguration<Article>
{
    public override void Configure(EntityTypeBuilder<Article> builder)
    {
        // Columns
        builder.Property(x => x.ArticleNumber).IsRequired();
        builder.Property(x => x.ArticleType).IsRequired();
        builder.Property(x => x.FriendlyName);
        builder.Property(x => x.Text1);
        builder.Property(x => x.Text2);
        builder.Property(x => x.Text3);
        builder.Property(x => x.Text4);
        builder.Property(x => x.Shape);

        var colorConverter = new ColorValueConverter();
        builder.Property(x => x.Color1)
            .IsRequired(false)
            .HasMaxLength(7)
            .HasConversion(colorConverter);

        builder.Property(x => x.Color2)
            .IsRequired(false)
            .HasMaxLength(7)
            .HasConversion(colorConverter);

        builder.Property(x => x.Color3)
            .IsRequired(false)
            .HasMaxLength(7)
            .HasConversion(colorConverter);

        builder.Property(x => x.Color4)
            .IsRequired(false)
            .HasMaxLength(7)
            .HasConversion(colorConverter);

        builder.Property(x => x.Hidden).IsRequired();

        // Indexes
        builder.HasIndex(x => x.ArticleNumber)
               .IsUnique();
        builder.HasIndex(x => x.ArticleType);

        base.Configure(builder);
    }
}