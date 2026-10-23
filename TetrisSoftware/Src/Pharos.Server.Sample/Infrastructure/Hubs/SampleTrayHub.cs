using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Sample.Infrastructure.Hubs;

[Authorize]
public class SampleTrayHub(
    IHubContext<SampleTrayHub, ISampleTrayHub> hubContext,
    ILogger<SampleTrayHub> logger) : EntityHub<SampleTrayHub, ISampleTrayHub, string>(hubContext, logger), ISampleTrayHub
{
    protected override string DefaultGroup => "sampleTray_default";

    public Task PositionStatusUpdated(List<string> trayIds)
    {
        return HubContext.Clients.Group(DefaultGroup).PositionStatusUpdated(trayIds);
    }

    public Task FreePositionAndErrorCountUpdated()
    {
        return HubContext.Clients.Group(DefaultGroup).FreePositionAndErrorCountUpdated();
    }
}