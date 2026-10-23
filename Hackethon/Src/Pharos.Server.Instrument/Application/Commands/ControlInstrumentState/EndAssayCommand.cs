using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;

public record EndAssayCommand(string InstrumentNumber, EndAssayFinishOptions FinishOption) : IRequest, IInstrumentCommand;
public enum EndAssayFinishOptions
{
    FinishAllSamplesScheduled,
    FinishAllDispensedTests
}

internal class EndAssayCommandHandler(ICommunicationSessionManager sessionManager)
    : ControlInstrumentStateCommandHandler<EndAssayCommand>(sessionManager)
{
    protected override Task Handle(EndAssayCommand command, CommunicationSession session, CancellationToken cancellationToken)
    {
        var request = new ControlInstrumentStateRequest
        {
            OrderedState = EnumOrderedInstrumentState.Standby,
            OrderedStateFinishOption = Map(command.FinishOption),
            OrderedStateRinseOption = EnumStateRinseOptions.PerformRinse
        };

        // Send request to instrument about change of state.
        return ControlInstrumentStateAsync(session.Connection, request, cancellationToken);
    }

    private static EnumStateFinishOptions Map(EndAssayFinishOptions finishOption)
    {
        return finishOption switch
        {
            EndAssayFinishOptions.FinishAllSamplesScheduled => EnumStateFinishOptions.FinishAllSamplesScheduled,
            EndAssayFinishOptions.FinishAllDispensedTests => EnumStateFinishOptions.FinishAllDispensedTests,
            _ => throw new ArgumentOutOfRangeException(nameof(finishOption), finishOption, null)
        };
    }
}