using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.InstrumentManagement.Models.Dashboard;

public interface ILisNotificationProvider : INotificationProvider
{
    event EventHandler LisStatusChanged;
    event Action<int> LisStatisticsChanged;
}

internal sealed class LisConnectionNotificationProvider : HubNotificationProvider, ILisNotificationProvider
{
    public LisConnectionNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Lis;

    public event EventHandler LisStatusChanged
    {
        add => Subscribe(nameof(LisStatusChanged), value);
        remove => Unsubscribe(nameof(LisStatusChanged), value);
    }

    public event Action<int> LisStatisticsChanged
    {
        add => Subscribe(nameof(LisStatisticsChanged), value);
        remove => Unsubscribe(nameof(LisStatisticsChanged), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(ILisConnectionHub.LisStatusChanged), OnLisStatusChanged);
        hubConnection.On<int>(nameof(ILisConnectionHub.LisStatisticsChanged), OnLisStatisticsChanged);
    }

    private void OnLisStatusChanged()
    {
        GetHandler<EventHandler>(nameof(LisStatusChanged))?.Invoke(this, EventArgs.Empty);
    }

    private void OnLisStatisticsChanged(int channelId)
    {
        GetHandler<Action<int>>(nameof(LisStatisticsChanged))?.Invoke(channelId);
    }
}