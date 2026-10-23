using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Sample.Infrastructure.Hubs;

[Authorize]
public class SampleTrayPositionHub(
    IHubContext<SampleTrayPositionHub, IEntityHub> hubContext,
    ILogger<SampleTrayPositionHub> logger) : EntityHub<SampleTrayPositionHub, IEntityHub>(hubContext, logger)
{
    protected override string DefaultGroup => "sampleTrayPosition_default";
}