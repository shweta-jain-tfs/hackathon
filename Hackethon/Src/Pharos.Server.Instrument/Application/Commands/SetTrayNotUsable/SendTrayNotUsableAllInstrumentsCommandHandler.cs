using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.SetTrayNotUsable;

namespace Pharos.Server.Instrument.Application.Commands.SetTrayNotUsable;

internal class SendTrayNotUsableAllInstrumentsCommandHandler(
    ICommunicationSessionManager sessionManager,
    IInstrumentManager instrumentManager,
    ILogger<SendTrayNotUsableAllInstrumentsCommandHandler> logger)
    : IRequestHandler<SendTrayNotUsableAllInstrumentsCommand>
{
    public async Task Handle(SendTrayNotUsableAllInstrumentsCommand request, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();

        foreach (var instrument in instruments)
        {
            if (instrument.State is InstrumentState.Offline or InstrumentState.Limited or InstrumentState.Synchronizing)
            {
                logger.LogDebug("Skipping instrument {Instrument}", instrument.SerialNumber);
                continue;
            }
            try
            {
                var session = sessionManager.GetSession(instrument.SerialNumber);
                if (session != null)
                {
                    await session.Connection.SetTrayNotUsableAsync(new TrayNotUsableRequest
                    {
                        TrayBarcode = request.TrayBarcode
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending TrayNotUsable to instrument {Instrument} for tray {TrayBarcode}",
                    instrument.SerialNumber, request.TrayBarcode);
            }
        }
    }
}
