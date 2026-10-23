using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.Models.Services;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.LoadList.Models;

public interface ILoadListNotificationProvider
{
    event EventHandler BottleLoadListUpdated;
    event EventHandler CarrierLoadListUpdated;
    event EventHandler CartridgeLoadListUpdated;
    event EventHandler DilutionPlateLoadListUpdated;
    event EventHandler SampleTubeLoadListUpdated;
}

internal class LoadListNotificationProvider : HubNotificationProvider, ILoadListNotificationProvider
{
    public LoadListNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.LoadList;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(ILoadListHub.BottleListChanged), OnBottleLoadListChanged);
        hubConnection.On(nameof(ILoadListHub.CarrierListChanged), OnCarrierLoadListChanged);
        hubConnection.On(nameof(ILoadListHub.CartridgeListChanged), OnCartridgeLoadListChanged);
        hubConnection.On(nameof(ILoadListHub.DilutionPlateListChanged), OnDilutionPlateLoadListChanged);
        hubConnection.On(nameof(ILoadListHub.SampleTubeListChanged), OnSampleTubeLoadListChanged);
    }

    public event EventHandler BottleLoadListUpdated
    {
        add => Subscribe(nameof(BottleLoadListUpdated), value);
        remove => Unsubscribe(nameof(BottleLoadListUpdated), value);
    }


    private void OnBottleLoadListChanged()
    {
        GetHandler<EventHandler>(nameof(BottleLoadListUpdated))?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler CarrierLoadListUpdated
    {
        add => Subscribe(nameof(CarrierLoadListUpdated), value);
        remove => Unsubscribe(nameof(CarrierLoadListUpdated), value);
    }

    private void OnCarrierLoadListChanged()
    {
        GetHandler<EventHandler>(nameof(CarrierLoadListUpdated))?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler CartridgeLoadListUpdated
    {
        add => Subscribe(nameof(CartridgeLoadListUpdated), value);
        remove => Unsubscribe(nameof(CartridgeLoadListUpdated), value);
    }
    private void OnCartridgeLoadListChanged()
    {
        GetHandler<EventHandler>(nameof(CartridgeLoadListUpdated))?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler DilutionPlateLoadListUpdated
    {
        add => Subscribe(nameof(DilutionPlateLoadListUpdated), value);
        remove => Unsubscribe(nameof(DilutionPlateLoadListUpdated), value);
    }
    private void OnDilutionPlateLoadListChanged()
    {
        GetHandler<EventHandler>(nameof(DilutionPlateLoadListUpdated))?.Invoke(this, EventArgs.Empty);
    }


    public event EventHandler SampleTubeLoadListUpdated
    {
        add => Subscribe(nameof(SampleTubeLoadListUpdated), value);
        remove => Unsubscribe(nameof(SampleTubeLoadListUpdated), value);
    }
    private void OnSampleTubeLoadListChanged()
    {
        GetHandler<EventHandler>(nameof(SampleTubeLoadListUpdated))?.Invoke(this, EventArgs.Empty);
    }

}
