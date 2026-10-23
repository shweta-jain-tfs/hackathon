using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Commands.UpdateMethodStatus;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;

internal record StartAssayCommand(string InstrumentNumber, bool CustomMadeRun = false) : IInstrumentCommand, IRequest;

internal class StartAssayCommandHandler(
    ICommunicationSessionManager sessionManager,
    ISender sender) : ControlInstrumentStateCommandHandler<StartAssayCommand>(sessionManager)
{
    protected override async Task Handle(StartAssayCommand command, CommunicationSession session, CancellationToken cancellationToken)
    {
        var request = new ControlInstrumentStateRequest
        {
            OrderedState = command.CustomMadeRun ? EnumOrderedInstrumentState.AssayCmr : EnumOrderedInstrumentState.Assay,
            OrderedStateFinishOption = EnumStateFinishOptions.UnspecifiedStateFinishOption,
            OrderedStateRinseOption = EnumStateRinseOptions.UnspecifiedStateRinseOption
        };

        // 1. Send information to instrument about how to run the assay.
        await sender.Send(new UpdateMethodStatusAllCommand(command.InstrumentNumber), cancellationToken);

        // 2. Send request to instrument about change of state.
        await ControlInstrumentStateAsync(session.Connection, request, cancellationToken);
    }
}