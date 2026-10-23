using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Lis.Infrastructure.Hubs;


[Authorize]
public class LisConnectionHub : HubBase<LisConnectionHub, ILisConnectionHub>, ILisConnectionHub
{
    private readonly IHubContext<LisConnectionHub, ILisConnectionHub> _hubContext;

    public LisConnectionHub(IHubContext<LisConnectionHub, ILisConnectionHub> hubContext,
        ILogger<HubBase<LisConnectionHub, ILisConnectionHub>> logger) : base(hubContext, logger)
    {
        _hubContext = hubContext;
    }

    protected override string DefaultGroup => "lis_state_default";

    public Task LisStatusChanged()
    {
        return _hubContext.Clients.Group(DefaultGroup).LisStatusChanged();
    }

    public Task LisStatisticsChanged(int channelId)
    {
        return _hubContext.Clients.Group(DefaultGroup).LisStatisticsChanged(channelId);
    }
}