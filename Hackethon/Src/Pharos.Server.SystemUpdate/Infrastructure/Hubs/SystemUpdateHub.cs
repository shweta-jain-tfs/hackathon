using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.SystemUpdate.Infrastructure.Hubs;

[Authorize]
public sealed class SystemUpdateHub(
    IHubContext<SystemUpdateHub, ISystemUpdateHub> hubContext,
    ILogger<SystemUpdateHub> logger)
    : HubBase<SystemUpdateHub, ISystemUpdateHub>(hubContext, logger), ISystemUpdateHub
{
    internal const string _groupName = "system_update_default";
    protected override string DefaultGroup => _groupName;

    public Task UpdatesAvailable(List<string> updatePackages)
    {
        logger.LogDebug("Broadcasting {Count} system update packages.", updatePackages.Count);

        return HubContext.Clients.Group(DefaultGroup).UpdatesAvailable(updatePackages);
    }

    public Task UpdateInProgress()
    {
        logger.LogDebug("Broadcasting system update in progress");

        return HubContext.Clients.Group(DefaultGroup).UpdateInProgress();
    }

    public Task UpdateFailed()
    {
        logger.LogDebug("Broadcasting system update failed");

        return HubContext.Clients.Group(DefaultGroup).UpdateFailed();
    }

    public Task UpdateOutcomeAcknowledged()
    {
        logger.LogDebug("Broadcasting system update outcome acknowledged");

        return HubContext.Clients.Group(DefaultGroup).UpdateOutcomeAcknowledged();
    }
}