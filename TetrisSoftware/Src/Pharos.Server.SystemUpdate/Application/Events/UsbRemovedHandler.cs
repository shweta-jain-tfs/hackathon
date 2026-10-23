using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure.Events;
using Pharos.Server.SystemUpdate.Application.Interfaces;

namespace Pharos.Server.SystemUpdate.Application.Events;

internal sealed class UsbRemovedHandler(
    IUpdateMediumStateStore stateStore,
    ISender sender,
    ISystemUpdateHub systemUpdateHub,
    ILogger<UsbRemovedHandler> logger
) : IDriveRemovedHandler
{
    public async Task OnDriveRemovedAsync(string driveName, CancellationToken cancellationToken)
    {
        var current = stateStore.Get();
        if (current is null)
        {
            return;
        }

        if (!string.Equals(current.DriveName, driveName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        stateStore.Clear();
        logger.LogInformation("Update medium removed: {Drive}. Clearing update notifications and task card.", driveName);

        try
        {
            await systemUpdateHub.UpdatesAvailable(new List<string>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed sending empty UpdatesAvailable notification after removal of {Drive}", driveName);
        }

        await sender.Send(new RemoveTaskCardCommand(TaskCardType.UpdatesAvailable), cancellationToken);
    }
}
