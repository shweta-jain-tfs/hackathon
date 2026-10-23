using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Reagents.Infrastructure.Hubs;

[Authorize]
public class InventoryHub(IHubContext<InventoryHub, IInventoryHub> hubContext, ILogger<InventoryHub> logger) : HubBase<InventoryHub, IInventoryHub>(hubContext, logger), IInventoryHub
{
    protected override string DefaultGroup => "inventory";

    public Task CarriersListChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).CarriersListChanged();
    }

    public Task CartridgesListChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).CartridgesListChanged();
    }

    public Task BottlesListChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).BottlesListChanged();
    }

    public Task DrawerDilutionPlatesChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).DrawerDilutionPlatesChanged();
    }

    public Task InternalDilutionPlatesChanged()
    {
        return HubContext.Clients.Group(DefaultGroup).InternalDilutionPlatesChanged();
    }
}
