using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.Models.Services;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.SystemUpdate.Notifications;

/// <summary>
/// SignalR based implementation of <see cref="ISystemUpdateNotificationProvider"/>.
/// </summary>
internal sealed class SystemUpdateNotificationProvider : HubNotificationProvider, ISystemUpdateNotificationProvider
{
    public SystemUpdateNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.SystemUpdate;

    public event EventHandler<List<string>> UpdatesAvailable
    {
        add => Subscribe(nameof(UpdatesAvailable), value);
        remove => Unsubscribe(nameof(UpdatesAvailable), value);
    }

    public event EventHandler? UpdateInProgress
    {
        add => Subscribe(nameof(UpdateInProgress), value);
        remove => Unsubscribe(nameof(UpdateInProgress), value);
    }

    public event EventHandler? UpdateFailed
    {
        add => Subscribe(nameof(UpdateFailed), value);
        remove => Unsubscribe(nameof(UpdateFailed), value);
    }

    public event EventHandler? UpdateOutcomeAcknowledged
    {
        add => Subscribe(nameof(UpdateOutcomeAcknowledged), value);
        remove => Unsubscribe(nameof(UpdateOutcomeAcknowledged), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<List<string>>(nameof(ISystemUpdateHub.UpdatesAvailable), OnUpdatesAvailable);
        hubConnection.On(nameof(ISystemUpdateHub.UpdateInProgress), OnUpdateInProgress);
        hubConnection.On(nameof(ISystemUpdateHub.UpdateFailed), OnUpdateFailed);
        hubConnection.On(nameof(ISystemUpdateHub.UpdateOutcomeAcknowledged), OnUpdateOutcomeAcknowledged);
    }

    private void OnUpdatesAvailable(List<string> updates)
    {
        GetHandler<EventHandler<List<string>>>(nameof(UpdatesAvailable))?.Invoke(this, updates);
    }

    private void OnUpdateFailed()
    {
        GetHandler<EventHandler>(nameof(UpdateFailed))?.Invoke(this, EventArgs.Empty);
    }

    private void OnUpdateInProgress()
    {
        GetHandler<EventHandler>(nameof(UpdateInProgress))?.Invoke(this, EventArgs.Empty);
    }

    private void OnUpdateOutcomeAcknowledged()
    {
        GetHandler<EventHandler>(nameof(UpdateOutcomeAcknowledged))?.Invoke(this, EventArgs.Empty);
    }
}
