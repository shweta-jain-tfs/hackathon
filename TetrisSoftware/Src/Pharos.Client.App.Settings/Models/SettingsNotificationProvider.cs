using Microsoft.AspNetCore.SignalR.Client;

namespace Pharos.Client.App.Settings.Models;

internal interface ISettingsNotificationProvider
{
    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}

internal sealed class SettingsChangedEventArgs(
    IReadOnlyCollection<string> updatedKeys,
    IReadOnlyCollection<string> deletedKeys,
    string? changedBy = null) : EventArgs
{
    public IReadOnlyCollection<string> UpdatedKeys { get; } = updatedKeys;

    public IReadOnlyCollection<string> DeletedKeys { get; } = deletedKeys;

    public string? ChangedBy { get; } = changedBy;

    public bool HasKeyDetails => UpdatedKeys.Count > 0 || DeletedKeys.Count > 0;
}

internal sealed class SettingsNotificationProvider(IHubConnectionFactory hubConnectionFactory)
    : HubNotificationProvider(hubConnectionFactory), ISettingsNotificationProvider
{
    protected override string HubRoute => HubRoutes.Settings;

    public event EventHandler<SettingsChangedEventArgs> SettingsChanged
    {
        add => Subscribe(nameof(SettingsChanged), value);
        remove => Unsubscribe(nameof(SettingsChanged), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        base.OnHubConnectionCreated(hubConnection);

        hubConnection.On<List<string>, List<string>, string?>(nameof(ISettingsHub.SettingsChanged), RefreshChangedKeys);
    }

    private void RefreshChangedKeys(List<string> updatedKeys, List<string> deletedKeys, string? changedBy)
    {
        NotifySettingsChanged(new SettingsChangedEventArgs(updatedKeys, deletedKeys, changedBy));
    }

    private void NotifySettingsChanged(SettingsChangedEventArgs args)
    {
        GetHandler<EventHandler<SettingsChangedEventArgs>>(nameof(SettingsChanged))?.Invoke(this, args);
    }
}
