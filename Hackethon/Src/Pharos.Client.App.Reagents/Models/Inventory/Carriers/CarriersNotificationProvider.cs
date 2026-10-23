using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Reagents.Models.Inventory.Carriers;
internal class CarriersNotificationProvider(IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), ICarriersNotificationProvider
{
    protected override string HubRoute => HubRoutes.Inventory;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(IInventoryHub.CarriersListChanged), OnCarriersListChanged);
    }

    public event Func<Task> CarriersListChanged
    {
        add => Subscribe(nameof(CarriersListChanged), value);
        remove => Unsubscribe(nameof(CarriersListChanged), value);
    }

    private void OnCarriersListChanged()
    {
        GetHandler<Func<Task>>(nameof(CarriersListChanged))?.Invoke();
    }

}
