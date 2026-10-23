using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Request.Infrastructure.Hubs;

[Authorize]
public class RequestHub(IHubContext<RequestHub, IRequestHub> hubContext, ILogger<RequestHub> logger) : EntityHub<RequestHub, IRequestHub>(hubContext, logger), IRequestHub
{
    protected override string DefaultGroup => "request_default";

    public Task RequestsDeleted(List<int> requestIds)
    {
        return HubContext.Clients.Group(DefaultGroup).RequestsDeleted(requestIds);
    }
}