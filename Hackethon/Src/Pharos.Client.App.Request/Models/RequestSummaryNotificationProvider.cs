using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Request.Models;

public interface IRequestSummaryNotificationProvider : IEntityNotificationProvider<RequestSummary>
{
    event EventHandler<List<int>>? RequestsDeleted;
}

internal sealed class RequestSummaryNotificationProvider : EntityNotificationProvider<RequestSummary>, IRequestSummaryNotificationProvider
{
    public RequestSummaryNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Request;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        base.OnHubConnectionCreated(hubConnection);

        hubConnection.On<List<int>>(nameof(IRequestHub.RequestsDeleted), OnRequestsDeleted);
    }

    private void OnRequestsDeleted(List<int> requestIds)
    {
        if (HasSubscribers)
        {
            GetHandler<EventHandler<List<int>>>(nameof(RequestsDeleted))?.Invoke(this, requestIds);
        }
    }

    public event EventHandler<List<int>>? RequestsDeleted
    {
        add => Subscribe(nameof(RequestsDeleted), value);
        remove => Unsubscribe(nameof(RequestsDeleted), value);
    }
}