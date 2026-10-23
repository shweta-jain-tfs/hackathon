using FluentValidation;

namespace Pharos.Server.LoadList.Application.Commands.CopyLoadListLevelsCommand;
public class CopyLoadListLevelsCommandValidator : AbstractValidator<CopyLoadListLevelsCommand>
{
    public CopyLoadListLevelsCommandValidator()
    {
        RuleFor(x => x.SourceInstrument)
            .NotEmpty()
            .WithMessage("Source instrument must be specified.");

        RuleFor(x => x.TargetInstruments)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one target instrument must be specified.");

        RuleFor(x => x.TargetInstruments)
            .Must((command, targets) => !targets.Contains(command.SourceInstrument))
            .WithMessage("Source instrument cannot be in the target instruments list.");
    }
}