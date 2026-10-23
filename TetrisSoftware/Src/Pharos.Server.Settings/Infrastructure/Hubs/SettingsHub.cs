using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Contracts.Security;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Settings.Infrastructure.Hubs;

[Authorize(Permissions.EditSystemSettings)]
public class SettingsHub(
    IHubContext<SettingsHub, ISettingsHub> hubContext,
    ILogger<SettingsHub> logger) : HubBase<SettingsHub, ISettingsHub>(hubContext, logger), ISettingsHub
{
    protected override string DefaultGroup => "settings_default";

    public Task SettingsChanged(List<string> updatedKeys, List<string> deletedKeys, string? changedBy)
    {
        Logger.LogDebug(
            "Sending settings changed message to clients in the group {HubGroup}. Updated keys: {UpdatedKeyCount}, deleted keys: {DeletedKeyCount}.",
            DefaultGroup,
            updatedKeys.Count,
            deletedKeys.Count);

        return HubContext.Clients.Group(DefaultGroup).SettingsChanged(updatedKeys, deletedKeys, changedBy);
    }
}
