using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Result.Models;

public interface ITestResultChartNotificationProvider : INotificationProvider
{
    event EventHandler<(string, string)> CurveControlChartUpdated;
    event EventHandler<(string, string)> CalibratorCurveChartUpdated;
    event EventHandler<(string InstrumentNumber, string MethodName)> QualityControlChartUpdated;
}

internal sealed class TestResultChartNotificationProvider(IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), ITestResultChartNotificationProvider
{
    protected override string HubRoute => HubRoutes.Result;

    public event EventHandler<(string, string)> CurveControlChartUpdated
    {
        add => Subscribe(nameof(CurveControlChartUpdated), value);
        remove => Unsubscribe(nameof(CurveControlChartUpdated), value);
    }

    public event EventHandler<(string, string)> CalibratorCurveChartUpdated
    {
        add => Subscribe(nameof(CalibratorCurveChartUpdated), value);
        remove => Unsubscribe(nameof(CalibratorCurveChartUpdated), value);
    }

    public event EventHandler<(string, string)> QualityControlChartUpdated
    {
        add => Subscribe(nameof(QualityControlChartUpdated), value);
        remove => Unsubscribe(nameof(QualityControlChartUpdated), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<string, string>(nameof(IResultHub.CurveControlChartUpdated), OnCurveControlChartUpdated);
        hubConnection.On<string, string>(nameof(IResultHub.CalibratorCurveChartUpdated), OnCalibratorCurveChartUpdated);
        hubConnection.On<string, string>(nameof(IResultHub.QualityControlChartUpdated), OnQualityControlChartUpdated);
    }

    private void OnCurveControlChartUpdated(string instrumentId, string methodName)
    {
        GetHandler<EventHandler<(string, string)>>(nameof(CurveControlChartUpdated))?.Invoke(this, (instrumentId, methodName));
    }
    private void OnCalibratorCurveChartUpdated(string instrumentId, string methodName)
    {
        GetHandler<EventHandler<(string, string)>>(nameof(CalibratorCurveChartUpdated))?.Invoke(this, (instrumentId, methodName));
    }
    private void OnQualityControlChartUpdated(string instrumentId, string methodName)
    {
        GetHandler<EventHandler<(string, string)>>(nameof(QualityControlChartUpdated))?.Invoke(this, (instrumentId, methodName));
    }
}