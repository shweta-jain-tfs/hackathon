using FluentValidation;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Services;

namespace Pharos.Server.TaskCard.Application.Commands;

internal class TaskCardCommandValidator : AbstractValidator<TaskCardCommand>
{
    public TaskCardCommandValidator(ITaskCardRules taskCardRules)
    {
        RuleFor(x => x.CardType).Must((command, type) => taskCardRules.ValidateInstrumentNumberAndCardType(command.InstrumentNumber, type));
    }
}

internal class RemoveTaskCardCommandValidator : AbstractValidator<RemoveTaskCardCommand>
{
    public RemoveTaskCardCommandValidator(ITaskCardRules taskCardRules)
    {
        RuleFor(x => x.CardType).Must((command, type) => taskCardRules.ValidateInstrumentNumberAndCardType(command.InstrumentNumber, type));
    }
}