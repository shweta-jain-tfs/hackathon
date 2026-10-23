using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;

namespace Pharos.Server.Infrastructure.Hubs;

public abstract class CurrentInstrumentHubBase<THub, TNotification> : HubBase<THub, TNotification>
    where THub : Hub<TNotification>
    where TNotification : class
{
    private readonly ICurrentInstrumentService _currentInstrumentService;

    protected CurrentInstrumentHubBase(
        ICurrentInstrumentService currentInstrumentService,
        IHubContext<THub, TNotification> hubContext,
        ILogger<CurrentInstrumentHubBase<THub, TNotification>> logger)
        : base(hubContext, logger)
    {
        _currentInstrumentService = currentInstrumentService;
    }

    protected override string DefaultGroup => "not_used";

    protected override Task AddClientToGroup()
    {
        var instrumentId = _currentInstrumentService.InstrumentId;
        return instrumentId == null ? Task.CompletedTask : Groups.AddToGroupAsync(Context.ConnectionId, instrumentId.Value.ToString(CultureInfo.InvariantCulture));
    }

    protected TNotification GetClientGroup(int instrumentId)
    {
        return HubContext.Clients.Group(instrumentId.ToString(CultureInfo.InvariantCulture));
    }
}
