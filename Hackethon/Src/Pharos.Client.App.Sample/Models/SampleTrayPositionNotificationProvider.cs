using Microsoft.AspNetCore.SignalR.Client;

namespace Pharos.Client.App.Sample.Models;

public interface ISampleTrayPositionNotificationProvider : INotificationProvider
{
    event Func<List<string>, Task> PositionStatusUpdated;
    event Func<Task> FreePositionAndErrorCountUpdated;
}

internal sealed class SampleTrayPositionNotificationProvider(IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), ISampleTrayPositionNotificationProvider
{
    protected override string HubRoute => HubRoutes.SampleTray;

    public event Func<List<string>, Task> PositionStatusUpdated
    {
        add => Subscribe(nameof(PositionStatusUpdated), value);
        remove => Unsubscribe(nameof(PositionStatusUpdated), value);
    }

    public event Func<Task> FreePositionAndErrorCountUpdated
    {
        add => Subscribe(nameof(FreePositionAndErrorCountUpdated), value);
        remove => Unsubscribe(nameof(FreePositionAndErrorCountUpdated), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<List<string>>(nameof(ISampleTrayHub.PositionStatusUpdated), OnPositionStatusUpdated);
        hubConnection.On(nameof(ISampleTrayHub.FreePositionAndErrorCountUpdated), OnFreePositionAndErrorCountUpdated);
    }

    private Task OnPositionStatusUpdated(List<string> trayIds)
    {
        return GetHandler<Func<List<string>, Task>>(nameof(PositionStatusUpdated))?.Invoke(trayIds) ?? Task.CompletedTask;
    }

    private Task OnFreePositionAndErrorCountUpdated()
    {
        return GetHandler<Func<Task>>(nameof(FreePositionAndErrorCountUpdated))?.Invoke() ?? Task.CompletedTask;
    }
}