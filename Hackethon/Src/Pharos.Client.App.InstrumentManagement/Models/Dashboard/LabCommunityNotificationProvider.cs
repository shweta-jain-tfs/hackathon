using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.InstrumentManagement.Models.Dashboard;

public interface ILabCommunityNotificationProvider : INotificationProvider
{
    event EventHandler LabCommunityStatusChanged;
}

internal sealed class LabCommunityNotificationProvider : HubNotificationProvider, ILabCommunityNotificationProvider
{
    public LabCommunityNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.LabCommunity;

    public event EventHandler LabCommunityStatusChanged
    {
        add => Subscribe(nameof(LabCommunityStatusChanged), value);
        remove => Unsubscribe(nameof(LabCommunityStatusChanged), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(ILabCommunityConnectionHub.LabCommunityStatusChanged), OnLabCommunityStatusChanged);
    }

    private void OnLabCommunityStatusChanged()
    {
        GetHandler<EventHandler>(nameof(LabCommunityStatusChanged))?.Invoke(this, EventArgs.Empty);
    }
}
