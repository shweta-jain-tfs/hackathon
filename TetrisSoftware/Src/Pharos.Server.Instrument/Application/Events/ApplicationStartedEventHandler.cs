using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Events;

internal sealed class ApplicationStartedEventHandler(
    IInstrumentManager instrumentManager,
    IMediator mediator,
    ILogger<ApplicationStartedEventHandler> logger) : INotificationHandler<ApplicationStartedEvent>
{
    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
#if !NSWAG
        await SyncOfflineTaskCards(cancellationToken);

        await SetupAssayScheduling(cancellationToken);
#endif
    }

    private async Task SyncOfflineTaskCards(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var instrument in instrumentManager.GetInstruments())
            {
                if (instrument is { Enabled: true, State: InstrumentState.Offline })
                {
                    await mediator.Send(new TaskCardCommand(TaskCardType.InstrumentSoftwareOffline, TaskCardState.Error, instrument.SerialNumber), cancellationToken);
                }
                else
                {
                    await mediator.Send(new RemoveTaskCardCommand(TaskCardType.InstrumentSoftwareOffline, instrument.SerialNumber), cancellationToken);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to synchronize instrument software offline task cards.");
        }
    }

    private async Task SetupAssayScheduling(CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting up assay scheduling");
        try
        {
            await mediator.Publish(new ReCalculateAssayScheduling(), cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to setup assay scheduling");
        }
    }
}

