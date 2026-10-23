using FluentValidation;

namespace Pharos.Server.Request.Application.Commands.RetestRejectedResultsCommand;

internal class RetestRejectedResultsCommandValidator : AbstractValidator<Server.Application.Request.RetestRejectedResultsCommand>
{
    public RetestRejectedResultsCommandValidator()
    {
        RuleFor(c => c.TestOrderIds)
            .NotEmpty()
            .WithMessage("At least one test order ID must be provided.");

        When(c => c.TestOrderIds.Count > 1, () =>
        {
            RuleFor(c => c.InstrumentDilution)
                .Must(d => d == null)
                .WithMessage("You can only specify one test order if you are changing the instrument dilution factor.");
        });
    }
}
