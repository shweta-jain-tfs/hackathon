using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Request.Models;

public interface IRequestDetailsNotificationProvider : IDetailsNotificationProvider<int>
{
    event EventHandler<List<int>>? RequestsDeleted;
}

public class RequestDetailsNotificationProvider : DetailsPageNotificationProvider<int>, IRequestDetailsNotificationProvider
{
    public RequestDetailsNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Request;

    public event EventHandler<List<int>>? RequestsDeleted
    {
        add => Subscribe(nameof(RequestsDeleted), value);
        remove => Unsubscribe(nameof(RequestsDeleted), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        base.OnHubConnectionCreated(hubConnection);
        hubConnection.On<List<int>>(nameof(IRequestHub.RequestsDeleted), OnRequestsDeleted);
    }

    private void OnRequestsDeleted(List<int> requestIds)
    {
        GetHandler<EventHandler<List<int>>>(nameof(RequestsDeleted))?.Invoke(this, requestIds);
    }
}