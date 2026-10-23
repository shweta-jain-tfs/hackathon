using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Request.Infrastructure.Hubs;

[Authorize]
public class TubeDefinitionHub(
    IHubContext<TubeDefinitionHub, IEntityHub> hubContext,
    ILogger<TubeDefinitionHub> logger) : EntityHub<TubeDefinitionHub, IEntityHub>(hubContext, logger)
{
    protected override string DefaultGroup => "tubedefinitions_default";
}
