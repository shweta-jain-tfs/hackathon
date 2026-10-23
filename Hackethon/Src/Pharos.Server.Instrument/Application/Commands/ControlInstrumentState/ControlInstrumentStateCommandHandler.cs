using MediatR;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Localization;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;

internal abstract class ControlInstrumentStateCommandHandler<TInstrumentCommand>(ICommunicationSessionManager sessionManager)
    : InstrumentCommandHandlerBase<TInstrumentCommand>(sessionManager) where TInstrumentCommand : IInstrumentCommand, IRequest
{
    protected static async Task ControlInstrumentStateAsync(IInstrumentConnection connection, ControlInstrumentStateRequest message, CancellationToken cancellationToken)
    {
        var response = await connection.ControlInstrumentStateAsync(message, cancellationToken);

        switch (response.Response)
        {
            case EnumInstrumentStateResponse.Ok:
                break;
            case EnumInstrumentStateResponse.StateUnreachable:
                throw new NotAllowedException(Translations.ErrorStateUnreachable);
            case EnumInstrumentStateResponse.CmrMissingReagents:
                throw new NotAllowedException(Translations.ErrorMissingReagentsCmr);
            default:
                throw new NotAllowedException(Translations.ErrorUnexpected);
        }
    }
}
