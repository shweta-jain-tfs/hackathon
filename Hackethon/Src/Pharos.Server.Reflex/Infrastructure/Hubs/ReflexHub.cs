using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure.Hubs;
using Pharos.Server.Reflex.Application.Interfaces;

namespace Pharos.Server.Reflex.Infrastructure.Hubs;

[Authorize]
public class ReflexHub(IHubContext<ReflexHub, IReflexHub> hubContext, ILogger<ReflexHub> logger) : EntityHub<ReflexHub, IReflexHub>(hubContext, logger), IReflexHub
{
    protected override string DefaultGroup => "reflex_default";
}