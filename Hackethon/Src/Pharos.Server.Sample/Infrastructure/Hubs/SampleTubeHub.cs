using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Sample.Infrastructure.Hubs;

[Authorize]
public class SampleTubeHub(
    IHubContext<SampleTubeHub, ISampleTubeHub> hubContext,
    ILogger<HubBase<SampleTubeHub, ISampleTubeHub>> logger)
    : HubBase<SampleTubeHub, ISampleTubeHub>(hubContext, logger), ISampleTubeHub
{
    protected override string DefaultGroup => "sampleTube_default";

    public Task SampleTubeUpdated(int requestId)
    {
        return HubContext.Clients.Group(DefaultGroup).SampleTubeUpdated(requestId);
    }
}
