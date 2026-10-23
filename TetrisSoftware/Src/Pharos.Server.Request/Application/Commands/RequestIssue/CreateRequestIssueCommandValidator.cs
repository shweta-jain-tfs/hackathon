using FluentValidation;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.RequestIssue;

internal class CreateRequestIssueCommandValidator : AbstractValidator<CreateRequestIssueCommand>
{
    public CreateRequestIssueCommandValidator()
    {
        RuleFor(x => x.RequestId).GreaterThan(0);
        RuleFor(x => x.IssueType).IsInEnum();
        RuleFor(x => x.Key).MaximumLength(50);

        When(x => x.IssueType == RequestIssueType.TestsRemainingLoadTube, () =>
        {
            // The key for TestsRemainingLoadTube is the instrument name, which is required to be non-empty
            RuleFor(x => x.Key).NotEmpty();
        });

        When(x => x.IssueType == RequestIssueType.TestsRemainingMoveTube, () =>
        {
            // The key for TestsRemainingMoveTube should be in the format of "From_Instrument{char31}To_Instrument"
            // The client splits on the char 31 to get the from and to instrument names, and displays them in the UI
            RuleFor(x => x.Key).NotEmpty().Matches($"^.+{Convert.ToChar(31)}.+$");
        });
    }
}