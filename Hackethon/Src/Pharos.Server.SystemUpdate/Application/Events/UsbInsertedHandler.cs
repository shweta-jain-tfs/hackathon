using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure.Events;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Events;

internal sealed class UsbInsertedHandler(
    IUpdateServiceFactory factory,
    IUpdateMediumStateStore stateStore,
    ISender sender,
    ISystemUpdateHub systemUpdateHub,
    IUpdateMediumValidator updateMediumValidator,
    ILogger<UsbInsertedHandler> logger
) : IDriveInsertedHandler
{
    public async Task OnDriveInsertedAsync(string driveName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(driveName))
        {
            logger.LogDebug("USB inserted event without drive name ignored.");
            return;
        }

        if (stateStore.Get() is not null)
        {
            logger.LogDebug("An update medium is already active; ignoring insertion of drive {Drive}.", driveName);
            return;
        }

        if (!updateMediumValidator.IsValidUpdateMedium(driveName))
        {
            logger.LogInformation("Drive {Drive} is not a valid update medium.", driveName);
            return;
        }

        stateStore.Set(new UpdateMediumState(driveName));

        List<string> updates;
        try
        {
            var service = factory.Resolve(UpdateSource.Usb);
            updates = service.GetAvailableUpdates(cancellationToken);

            if (updates.Count == 0)
            {
                logger.LogInformation("No updates found on drive {Drive}.", driveName);
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed while discovering updates on drive {Drive}", driveName);
            return;
        }

        logger.LogInformation("Update medium registered: {Drive} with {Count} packages.", driveName, updates.Count);

        try
        {
            await systemUpdateHub.UpdatesAvailable(updates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed sending UpdatesAvailable notification for drive {Drive}", driveName);
        }

        await sender.Send(new TaskCardCommand(TaskCardType.UpdatesAvailable, TaskCardState.Information), cancellationToken);
    }
}