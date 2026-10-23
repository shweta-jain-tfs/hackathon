using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Parameters.QualityControl.Models;

public interface IQualityControlBarcodeFormatNotificationProvider : IRefreshNotificationProvider;

public class QualityControlBarcodeFormatNotificationProvider : HubRefreshNotificationProvider, IQualityControlBarcodeFormatNotificationProvider
{
    public QualityControlBarcodeFormatNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.BarcodeFormat;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        base.OnHubConnectionCreated(hubConnection);
        hubConnection.On<int>(nameof(IBarcodeFormatHub.Created), Refresh);
        hubConnection.On<int>(nameof(IBarcodeFormatHub.Updated), Refresh);
        hubConnection.On<int>(nameof(IBarcodeFormatHub.Deleted), Refresh);
    }

    private void Refresh(int id)
    {
        OnRefreshNeeded();
    }
}