using FluentValidation;
using Pharos.Server.Domain;

namespace Pharos.Server.Instrument.Application.Commands.StartSession;

internal class StartSessionCommandValidator : AbstractValidator<StartSessionCommand>
{
    public StartSessionCommandValidator()
    {
        RuleFor(c => c.SessionId).NotEqual(Guid.Empty);
        RuleFor(c => c.InstrumentNumber).NotEmpty();
        RuleFor(c => c.InstrumentServicePort).NotEmpty();

        RuleFor(c => c.InstrumentProtocolVersion)
            .Matches(@"\d+\.\d+\.\d+")
            .WithMessage("The protocol version number must match semantic versioning format: MAJOR.MINOR.PATCH");

        RuleFor(c => c.InstrumentSoftwareVersion)
            .MaximumLength(Constants.InstrumentSoftwareVersionLength)
            .Matches(@"\d+\.\d+\.\d+")
            .WithMessage("The instrument software version number must match semantic versioning format: MAJOR.MINOR.PATCH");
    }
}