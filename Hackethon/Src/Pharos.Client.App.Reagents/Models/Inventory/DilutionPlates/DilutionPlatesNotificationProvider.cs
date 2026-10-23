using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Reagents.Models.Inventory.DilutionPlates;

internal class DilutionPlatesNotificationProvider(IHubConnectionFactory hubConnectionFactory)
    : HubNotificationProvider(hubConnectionFactory), IDilutionPlatesNotificationProvider
{
    protected override string HubRoute => HubRoutes.Inventory;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(IInventoryHub.DrawerDilutionPlatesChanged), OnDrawerDilutionPlatesChanged);
        hubConnection.On(nameof(IInventoryHub.InternalDilutionPlatesChanged), OnInternalDilutionPlatesChanged);
    }

    public event Func<Task> DrawerDilutionPlatesChanged
    {
        add => Subscribe(nameof(DrawerDilutionPlatesChanged), value);
        remove => Unsubscribe(nameof(DrawerDilutionPlatesChanged), value);
    }

    public event Func<Task> InternalDilutionPlatesChanged
    {
        add => Subscribe(nameof(InternalDilutionPlatesChanged), value);
        remove => Unsubscribe(nameof(InternalDilutionPlatesChanged), value);
    }

    private void OnDrawerDilutionPlatesChanged()
    {
        GetHandler<Func<Task>>(nameof(DrawerDilutionPlatesChanged))?.Invoke();
    }

    private void OnInternalDilutionPlatesChanged()
    {
        GetHandler<Func<Task>>(nameof(InternalDilutionPlatesChanged))?.Invoke();
    }
}