using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;

internal record StandbyCommand(string InstrumentNumber) : IInstrumentCommand, IRequest;

internal class StandbyCommandHandler(ICommunicationSessionManager sessionManager)
    : ControlInstrumentStateCommandHandler<StandbyCommand>(sessionManager)
{
    protected override Task Handle(StandbyCommand command, CommunicationSession session, CancellationToken cancellationToken)
    {
        var request = new ControlInstrumentStateRequest
        {
            OrderedState = EnumOrderedInstrumentState.Standby,
            OrderedStateFinishOption = EnumStateFinishOptions.UnspecifiedStateFinishOption,
            OrderedStateRinseOption = EnumStateRinseOptions.UnspecifiedStateRinseOption
        };

        // Send request to instrument about change of state.
        return ControlInstrumentStateAsync(session.Connection, request, cancellationToken);
    }
}
