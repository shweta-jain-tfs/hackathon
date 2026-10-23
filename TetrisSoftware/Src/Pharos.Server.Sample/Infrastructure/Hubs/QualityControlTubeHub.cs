using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Sample.Infrastructure.Hubs;

[Authorize]
public class QualityControlTubeHub(
    IHubContext<QualityControlTubeHub, IQualityControlTubeHub> hubContext,
    ILogger<HubBase<QualityControlTubeHub, IQualityControlTubeHub>> logger)
    : HubBase<QualityControlTubeHub, IQualityControlTubeHub>(hubContext, logger), IQualityControlTubeHub
{
    protected override string DefaultGroup => "qualityControlTube_default";

    public Task TubeUpdated(string tubeId)
    {
        return HubContext.Clients.Group(DefaultGroup).TubeUpdated(tubeId);
    }
}
