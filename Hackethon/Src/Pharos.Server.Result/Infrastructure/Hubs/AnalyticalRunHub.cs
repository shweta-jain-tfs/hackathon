using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Result.Infrastructure.Hubs;
public class AnalyticalRunHub(
    IHubContext<AnalyticalRunHub, IEntityHub> hubContext,
    ILogger<AnalyticalRunHub> logger) : EntityHub<AnalyticalRunHub, IEntityHub>(hubContext, logger)
{
    protected override string DefaultGroup => "run_default";
  
}