using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.LabCommunity.Infrastructure.Hubs;

[Authorize]
public sealed class LabCommunityConnectionHub(
    IHubContext<LabCommunityConnectionHub, ILabCommunityConnectionHub> hubContext, ILogger<LabCommunityConnectionHub> logger)
    : HubBase<LabCommunityConnectionHub, ILabCommunityConnectionHub>(hubContext, logger), ILabCommunityConnectionHub
{
    internal const string _groupName = "labcommunity_default";

    protected override string DefaultGroup => _groupName;

    public Task LabCommunityStatusChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).LabCommunityStatusChanged();
    }
}