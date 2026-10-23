using FluentValidation;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

internal class IntendedMarketValidator : AbstractValidator<IntendedMarket>
{
    public IntendedMarketValidator()
    {
        RuleFor(x => x.Id).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(IntendedMarket.NameLength);
    }
}
