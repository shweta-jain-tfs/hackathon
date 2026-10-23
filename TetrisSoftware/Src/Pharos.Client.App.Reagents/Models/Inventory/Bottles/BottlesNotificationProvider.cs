using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Reagents.Models.Inventory.Bottles;
internal class BottlesNotificationPovider(IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), IBottlesNotificationProvider
{
    protected override string HubRoute => HubRoutes.Inventory;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(IInventoryHub.BottlesListChanged), OnBottlesListChanged);
    }

    public event Func<Task> BottlesListChanged
    {
        add => Subscribe(nameof(BottlesListChanged), value);
        remove => Unsubscribe(nameof(BottlesListChanged), value);
    }

    private void OnBottlesListChanged()
    {
        GetHandler<Func<Task>>(nameof(BottlesListChanged))?.Invoke();
    }
}
