using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentTechnologies;
using Pharos.Server.Instrument.Application.Commands.SendActiveMethods;
using Pharos.Server.Instrument.Application.Commands.UpdateMethods;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Events;

internal class MethodsUpdatedEventHandler(
    IInstrumentDbContext dbContext,
    ISender sender,
    ICommunicationSessionManager sessionManager,
    ICurrentInstrumentServerHub hub,
    IInstrumentHub instrumentHub,
    ILogger<MethodsUpdatedEventHandler> logger) :
    INotificationHandler<MethodsUpdatedEvent>,
    INotificationHandler<MethodEnabledEvent>
{
    public async Task Handle(MethodsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await HandleMethodsChanged(cancellationToken);
    }

    public async Task Handle(MethodEnabledEvent notification, CancellationToken cancellationToken)
    {
        await HandleMethodsChanged(cancellationToken);
    }

    private async Task HandleMethodsChanged(CancellationToken cancellationToken)
    {
        var instruments = await dbContext.Instruments
            .Select(x => new { x.Id, x.SerialNumber })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // update database information first for all instruments
        foreach (var instrument in instruments)
        {
            await sender.Send(new InitializeInstrumentTechnologiesCommand(instrument.Id), cancellationToken);
            await hub.InstrumentMethodsChanged(instrument.Id);
            await instrumentHub.InstrumentTechnologiesChanged(instrument.Id);
        }

        foreach (var instrument in instruments)
        {
            var session = sessionManager.GetSession(instrument.SerialNumber);

            // Only send command if the instrument is connected
            if (session is not { CompatibilityStatus: EnumCompatibilityStatus.FullyCompatible })
            {
                continue;
            }

            try
            {
                await sender.Send(new UpdateMethodsCommand(instrument.SerialNumber), cancellationToken);
                await sender.Send(new SendActiveMethodsCommand(instrument.SerialNumber), cancellationToken);
            }
            catch (Exception e)
            {
                // we log the error but continue with other instruments
                logger.LogError(e, "Error updating methods on instrument {Instrument}", instrument.SerialNumber);
            }
        }
    }
}
