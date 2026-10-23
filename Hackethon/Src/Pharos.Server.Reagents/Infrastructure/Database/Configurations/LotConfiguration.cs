using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Reagents.Infrastructure.Database.Configurations;

internal class LotConfiguration : BaseEntityConfiguration<Lot>
{
    public override void Configure(EntityTypeBuilder<Lot> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ArticleType)
            .IsRequired();

        builder.Property(x => x.LotNumber)
            .IsRequired();

        builder.Property(x => x.ExpiryDate)
            .IsRequired();

        builder.HasDiscriminator<ArticleType>("OrderType")
            .HasValue<Lot>(ArticleType.CalibratorCartridge)
            .HasValue<Lot>(ArticleType.ConjugateCartridge)
            .HasValue<Lot>(ArticleType.CurveControlCartridge)
            .HasValue<Lot>(ArticleType.DevelopmentSolutionCartridge)
            .HasValue<Lot>(ArticleType.DiluentCartridge)
            .HasValue<Lot>(ArticleType.DilutionWellCarrier)
            .HasValue<Lot>(ArticleType.EliaCalibratorWellCarrier)
            .HasValue<Lot>(ArticleType.EliaWellCarrier)
            .HasValue<Lot>(ArticleType.FluoroCVial)
            .HasValue<Lot>(ArticleType.ImmunoCapCarrier)
            .HasValue<Lot>(ArticleType.MaintenanceConcentrateBottle)
            .HasValue<Lot>(ArticleType.StopSolutionBottle)
            .HasValue<Lot>(ArticleType.WashAdditiveBottle)
            .HasValue<Lot>(ArticleType.WashConcentrateBottle);

        builder.HasIndex(x => x.LotNumber).IsUnique();
    }
}
