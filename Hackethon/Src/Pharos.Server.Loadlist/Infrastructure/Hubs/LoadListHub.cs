using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.LoadList.Infrastructure.Hubs;

[Authorize]
public class LoadListHub(
    IHubContext<LoadListHub, ILoadListHub> hubContext,
    ILogger<LoadListHub> logger) : HubBase<LoadListHub, ILoadListHub>(hubContext, logger), ILoadListHub
{
    protected override string DefaultGroup => "loadlist_default";

    public Task BottleListChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).BottleListChanged();
    }

    public Task CarrierListChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).CarrierListChanged();
    }

    public Task CartridgeListChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).CartridgeListChanged();
    }

    public Task DilutionPlateListChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).DilutionPlateListChanged();
    }

    public Task SampleTubeListChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).SampleTubeListChanged();
    }
}