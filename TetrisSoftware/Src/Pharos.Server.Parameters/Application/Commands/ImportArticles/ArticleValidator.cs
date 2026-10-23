using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

internal class ArticleValidator : AbstractValidator<Article>
{
    public ArticleValidator()
    {
        RuleFor(x => x.ArticleType)
            .Must(x => (int)x == -1 || Enum.GetValues<ArticleType>().Contains(x))
            .WithMessage(x => $"The article type {x.ArticleType} is not supported: Barcode = {x.ArticleNumber}, Name = '{x.FullName}'.");

        RuleFor(x => x.ArticleNumber)
            .Length(Constants.ArticleNumberLength)
            .Matches("[A-Z0-9]{3}")
            .NotEqual("000");

        RuleFor(x => x.FullName).MaximumLength(Article.FullNameLength);
        RuleFor(x => x.FriendlyName).MaximumLength(Article.FriendlyNameLength);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 9999);

        RuleFor(x => x.MethodName).MaximumLength(Constants.MethodNameLength);

        RuleFor(x => x.MaxPiercedHoles).InclusiveBetween(1, 2);

        RuleFor(x => x.Shape).IsInEnum();

        RuleFor(x => x.Text1).MaximumLength(Article.TextFieldLength);
        RuleFor(x => x.Text2).MaximumLength(Article.TextFieldLength);
        RuleFor(x => x.Text3).MaximumLength(Article.TextFieldLength);
        RuleFor(x => x.Text4).MaximumLength(Article.TextFieldLength);

        When(
            x => new[]
            {
                ArticleType.CalibratorCartridge,
                ArticleType.CurveControlCartridge
            }.Contains(x.ArticleType),
            () => RuleFor(x => x.MethodName).NotEmpty().WithMessage(x => $"Method Name is missing ({x.ArticleNumber}, {x.FullName})."));

        When(x => x.ArticleType.Equals(ArticleType.ImmunoCapCarrier), () => RuleFor(x => x.Identity).MaximumLength(5).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.DiluentCartridge), () => RuleFor(x => x.Identity).MaximumLength(Article.IdentityLength).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.EliaWellCarrier), () => RuleFor(x => x.Identity).MaximumLength(5).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.EliaCalibratorWellCarrier), () => RuleFor(x => x.Identity).MaximumLength(5).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.DilutionWellCarrier), () => RuleFor(x => x.Identity).MaximumLength(5).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.QualityControlCartridge), () => RuleFor(x => x.Identity).MaximumLength(16).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.ConjugateCartridge), () => RuleFor(x => x.Identity).MaximumLength(4).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.WashConcentrateBottle), () => RuleFor(x => x.Identity).MaximumLength(8).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.WashAdditiveBottle), () => RuleFor(x => x.Identity).MaximumLength(8).NotEmpty());
        When(x => x.ArticleType.Equals(ArticleType.DevelopmentSolutionCartridge), () => RuleFor(x => x.Identity).MaximumLength(8));
        When(x => x.ArticleType.Equals(ArticleType.QualityControlTube), () => RuleFor(x => x.Identity).MaximumLength(16).NotEmpty());

        When(x => x.MaxDaysInInstrument != null, () => RuleFor(x => x.MaxDaysInInstrument).InclusiveBetween(0, 999));
        When(x => x.MaxHoursInInstrument != null, () => RuleFor(x => x.MaxHoursInInstrument).InclusiveBetween(0, 999));

        When(
            x => new[] {
                ArticleType.CalibratorCartridge,
                ArticleType.CurveControlCartridge
            }.Contains(x.ArticleType),
            () => RuleFor(x => x.CartridgeCells).NotEmpty().WithMessage(x => $"Calibrator and curve control cartridges must have cell identities ({x.ArticleNumber}, {x.FullName})."));

        RuleForEach(x => x.CartridgeCells).SetValidator(new CartridgeCellIdentityValidator());
    }
}

internal class CartridgeCellIdentityValidator : AbstractValidator<CartridgeCellIdentity>
{
    public CartridgeCellIdentityValidator()
    {
        RuleFor(x => x.CellPosition).InclusiveBetween(1, 12);
        RuleFor(x => x.CellIdentity).MaximumLength(CartridgeCellIdentity.CellIdentityLength);
    }
}