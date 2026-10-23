using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.Models.Services;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Quality.Models;

public sealed class QualityControlTubeNotificationProvider(IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), IQualityControlTubeNotificationProvider
{
    protected override string HubRoute => HubRoutes.QualityControlTube;

    public event EventHandler<string> TubeUpdated
    {
        add => Subscribe(nameof(TubeUpdated), value);
        remove => Unsubscribe(nameof(TubeUpdated), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<string>(nameof(IQualityControlTubeHub.TubeUpdated), OnTubeUpdated);
    }

    private void OnTubeUpdated(string tubeId)
    {
        GetHandler<EventHandler<string>>(nameof(TubeUpdated))?.Invoke(this, tubeId);
    }
}