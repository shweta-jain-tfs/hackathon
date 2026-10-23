using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Reagents.Models.Inventory.Cartridges;
internal class CartridgesNotificationProvider(IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), ICartridgesNotificationProvider
{
    protected override string HubRoute => HubRoutes.Inventory;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(IInventoryHub.CartridgesListChanged), OnCartridgesListChanged);
    }

    public event Func<Task> CartridgesListChanged
    {
        add => Subscribe(nameof(CartridgesListChanged), value);
        remove => Unsubscribe(nameof(CartridgesListChanged), value);
    }

    private void OnCartridgesListChanged()
    {
        GetHandler<Func<Task>>(nameof(CartridgesListChanged))?.Invoke();
    }
}
