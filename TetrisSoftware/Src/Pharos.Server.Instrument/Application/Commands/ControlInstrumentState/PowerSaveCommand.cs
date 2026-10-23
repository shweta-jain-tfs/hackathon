using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;

internal record PowerSaveCommand(string InstrumentNumber) : IRequest, IInstrumentCommand;

internal class PowerSaveCommandHandler(
    ICommunicationSessionManager sessionManager) : ControlInstrumentStateCommandHandler<PowerSaveCommand>(sessionManager)
{
    protected override Task Handle(PowerSaveCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var message = new ControlInstrumentStateRequest
        {
            OrderedState = EnumOrderedInstrumentState.PowerSaving,
            OrderedStateFinishOption = EnumStateFinishOptions.UnspecifiedStateFinishOption,
            OrderedStateRinseOption = EnumStateRinseOptions.UnspecifiedStateRinseOption
        };

        // Send request to instrument about change of state.
        return ControlInstrumentStateAsync(session.Connection, message, cancellationToken);
    }
}
