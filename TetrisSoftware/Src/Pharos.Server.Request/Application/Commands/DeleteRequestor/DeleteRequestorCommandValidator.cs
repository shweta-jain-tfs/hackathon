using FluentValidation;

namespace Pharos.Server.Request.Application.Commands.DeleteRequestor;

internal class DeleteRequestorCommandValidator : AbstractValidator<DeleteRequestorCommand>
{
    public DeleteRequestorCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("At least one requestor ID must be provided for deletion.");

        RuleFor(x => x.Ids)
            .Must(ids => ids.All(id => id > 0))
            .WithMessage("All requestor IDs must be positive numbers.");

        RuleFor(x => x.Ids)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate requestor IDs are not allowed.");
    }
}