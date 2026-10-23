using FluentValidation;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Commands.ProcessModuleInformation;
internal class ProcessModuleInformationCommandValidator : AbstractValidator<ProcessModuleInformationCommand>
{
    public ProcessModuleInformationCommandValidator()
    {
        RuleFor(c => c.Message.Type).NotEqual(EnumInstrumentType.UnspecifiedInstrumentType);
        RuleFor(c => c.Message.SerialNumber).NotEmpty();
    }
}
