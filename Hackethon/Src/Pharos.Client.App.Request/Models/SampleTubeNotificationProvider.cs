using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Request.Models;

public sealed class SampleTubeNotificationProvider(IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), ISampleTubeNotificationProvider
{
    protected override string HubRoute => HubRoutes.SampleTube;
    public event EventHandler<int> SampleTubeUpdated
    {
        add => Subscribe(nameof(SampleTubeUpdated), value);
        remove => Unsubscribe(nameof(SampleTubeUpdated), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<int>(nameof(ISampleTubeHub.SampleTubeUpdated), OnSampleTubeUpdated);
    }

    private void OnSampleTubeUpdated(int requestId)
    {
        GetHandler<EventHandler<int>>(nameof(SampleTubeUpdated))?.Invoke(this, requestId);
    }
}