using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Commands.UpdateArticles;

internal record UpdateArticlesAllInstrumentsCommand : IRequest;

internal class UpdateArticlesAllInstrumentsCommandHandler(
    IInstrumentManager instrumentManager,
    ISender sender,
    ILogger<UpdateArticlesAllInstrumentsCommandHandler> logger) : IRequestHandler<UpdateArticlesAllInstrumentsCommand>
{
    public async Task Handle(UpdateArticlesAllInstrumentsCommand request, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();

        foreach (var instrument in instruments)
        {
            if (instrument.State is InstrumentState.Offline or InstrumentState.Limited)
            {
                logger.LogDebug("Skipping instrument {Instrument}", instrument.SerialNumber);
                continue;
            }

            try
            {
                await sender.Send(new UpdateArticlesCommand(instrument.SerialNumber, null), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating articles on instrument {Instrument}", instrument.SerialNumber);
            }
        }
    }
}