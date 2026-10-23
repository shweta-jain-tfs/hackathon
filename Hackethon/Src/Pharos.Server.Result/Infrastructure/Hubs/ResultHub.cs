using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Result.Infrastructure.Hubs;

[Authorize]
public class ResultHub(
    IHubContext<ResultHub, IResultHub> hubContext,
    ILogger<ResultHub> logger) : EntityHub<ResultHub, IResultHub>(hubContext, logger), IResultHub
{
    protected override string DefaultGroup => "result_default";

    public Task CurveControlChartUpdated(string instrumentSerial, string methodName)
    {
        return HubContext.Clients.Group(DefaultGroup).CurveControlChartUpdated(instrumentSerial, methodName);
    }

    public Task CalibratorCurveChartUpdated(string instrumentSerial, string methodName)
    {
        return HubContext.Clients.Group(DefaultGroup).CalibratorCurveChartUpdated(instrumentSerial, methodName);
    }
    public Task QualityControlChartUpdated(string instrumentSerial, string methodName)
    {
        return HubContext.Clients.Group(DefaultGroup).QualityControlChartUpdated(instrumentSerial, methodName);
    }
}