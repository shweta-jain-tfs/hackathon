using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;

internal record PowerOffCommand(string InstrumentNumber) : IRequest, IInstrumentCommand;

internal class PowerOffCommandHandler(
    ICommunicationSessionManager sessionManager) : ControlInstrumentStateCommandHandler<PowerOffCommand>(sessionManager)
{
    protected override Task Handle(PowerOffCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var message = new ControlInstrumentStateRequest
        {
            OrderedState = EnumOrderedInstrumentState.PowerOff,
            OrderedStateFinishOption = EnumStateFinishOptions.UnspecifiedStateFinishOption,
            OrderedStateRinseOption = EnumStateRinseOptions.UnspecifiedStateRinseOption
        };

        // Send request to instrument about change of state.
        return ControlInstrumentStateAsync(session.Connection, message, cancellationToken);
    }
}
