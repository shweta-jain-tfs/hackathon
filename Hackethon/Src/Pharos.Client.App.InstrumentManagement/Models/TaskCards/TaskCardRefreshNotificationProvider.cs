using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.InstrumentManagement.Models.TaskCards;

public class TaskCardRefreshNotificationProvider : HubRefreshNotificationProvider, ITaskCardRefreshNotificationProvider
{
    public TaskCardRefreshNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.TaskCard;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        base.OnHubConnectionCreated(hubConnection);
        hubConnection.On(nameof(ITaskCardHub.TaskCardsUpdated), OnRefreshNeeded);
    }
}
