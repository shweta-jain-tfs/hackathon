using FluentValidation;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

internal class PackageValidator : AbstractValidator<Package>
{
    public PackageValidator()
    {
        RuleFor(x => x.PackageBarcode)
            .Length(Package.PackageBarcodeLength)
            .Matches("[A-Z0-9]{3}")
            .NotEqual("000");

        RuleFor(x => x.OrderingNumber)
            .MaximumLength(15)
            .NotEmpty();

        RuleFor(x => x.PackageFullName).MaximumLength(55);
        RuleFor(x => x.IntendedMarket).SetValidator(new IntendedMarketValidator());
        RuleForEach(x => x.PackageItems).SetValidator(new PackageItemValidator());
    }
}

internal class PackageItemValidator : AbstractValidator<PackageItem>
{
    public PackageItemValidator()
    {
        RuleFor(x => x.ArticleAmount).InclusiveBetween(1, 9999).NotEmpty();
    }
}