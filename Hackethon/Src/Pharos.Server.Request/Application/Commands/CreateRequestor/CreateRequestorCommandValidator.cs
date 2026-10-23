using FluentValidation;
using Pharos.Server.Domain;

namespace Pharos.Server.Request.Application.Commands.CreateRequestor;

public class CreateRequestorCommandValidator : AbstractValidator<CreateRequestorCommand>
{
    public CreateRequestorCommandValidator()
    {
        RuleFor(x => x.RequestorId)
            .NotEmpty()
            .WithMessage("Requestor ID is required.")
            .MaximumLength(Constants.RequestorIdLength)
            .WithMessage($"Requestor ID cannot exceed {Constants.RequestorIdLength} characters.");

        RuleFor(x => x.Name)
            .MaximumLength(Constants.NameLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage($"Name cannot exceed {Constants.NameLength} characters.");

        RuleFor(x => x.Address)
            .MaximumLength(Constants.AddressLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Address))
            .WithMessage($"Address cannot exceed {Constants.AddressLength} characters.");

        RuleFor(x => x.Comment)
            .MaximumLength(Constants.CommentLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment))
            .WithMessage($"Comment cannot exceed {Constants.CommentLength} characters.");

        RuleFor(x => x.ZipCode)
            .MaximumLength(Constants.ZipCodeLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ZipCode))
            .WithMessage($"Zip code cannot exceed {Constants.ZipCodeLength} characters.");

        RuleFor(x => x.OrderingPhysicianCode)
            .MaximumLength(Constants.OrderingPhysicianCodeLength)
            .When(x => !string.IsNullOrWhiteSpace(x.OrderingPhysicianCode))
            .WithMessage($"Ordering physician code cannot exceed {Constants.OrderingPhysicianCodeLength} characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(Constants.PhoneNumberLength)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage($"Phone number cannot exceed {Constants.PhoneNumberLength} characters.");

        RuleFor(x => x.ShortcutCode)
            .MaximumLength(Constants.ShortcutCodeLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ShortcutCode))
            .WithMessage($"Shortcut code cannot exceed {Constants.ShortcutCodeLength} characters.");

    }
}