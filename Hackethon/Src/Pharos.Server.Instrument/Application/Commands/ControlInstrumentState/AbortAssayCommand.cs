using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;

internal record AbortAssayCommand(string InstrumentNumber) : IRequest, IInstrumentCommand;

internal class AbortAssayCommandHandler(ICommunicationSessionManager sessionManager)
    : ControlInstrumentStateCommandHandler<AbortAssayCommand>(sessionManager)
{
    protected override Task Handle(AbortAssayCommand command, CommunicationSession session, CancellationToken cancellationToken)
    {
        var request = new ControlInstrumentStateRequest
        {
            OrderedState = EnumOrderedInstrumentState.Standby,
            OrderedStateFinishOption = EnumStateFinishOptions.AbortAssayProcess,
            OrderedStateRinseOption = EnumStateRinseOptions.UnspecifiedStateRinseOption
        };

        // Send request to instrument about change of state.
        return ControlInstrumentStateAsync(session.Connection, request, cancellationToken);
    }
}