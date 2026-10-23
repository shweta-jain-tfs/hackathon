using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.Models.Services;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Maintenance.Models;

public interface IMaintenanceRefreshNotificationProvider : IRefreshNotificationProvider;

public class MaintenanceRefreshNotificationProvider : HubRefreshNotificationProvider, IMaintenanceRefreshNotificationProvider
{
    public MaintenanceRefreshNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Maintenance;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        base.OnHubConnectionCreated(hubConnection);
        hubConnection.On(nameof(IMaintenanceActivityHub.MaintenanceActivitiesUpdated), OnRefreshNeeded);
    }
}