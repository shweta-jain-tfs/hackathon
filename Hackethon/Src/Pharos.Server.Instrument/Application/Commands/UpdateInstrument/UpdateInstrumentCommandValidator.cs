using FluentValidation;
using Pharos.Server.Domain;

namespace Pharos.Server.Instrument.Application.Commands.UpdateInstrument;

internal class UpdateInstrumentCommandValidator : AbstractValidator<UpdateInstrumentCommand>
{
    public UpdateInstrumentCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(Constants.InstrumentDisplayNameLength);
    }
}