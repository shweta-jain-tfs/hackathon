using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Maintenance.Infrastructure.Hubs;

[Authorize]
public class MaintenanceActivityHub(IHubContext<MaintenanceActivityHub, IMaintenanceActivityHub> hubContext, ILogger<MaintenanceActivityHub> logger) : HubBase<MaintenanceActivityHub, IMaintenanceActivityHub>(hubContext, logger), IMaintenanceActivityHub
{
    protected override string DefaultGroup => "maintenanceactivity_default";

    public Task MaintenanceActivitiesUpdated()
    {
        return HubContext.Clients.Group(DefaultGroup).MaintenanceActivitiesUpdated();
    }
}