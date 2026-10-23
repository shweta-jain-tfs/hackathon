using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Contracts.Security;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Instrument.Infrastructure.Hubs;

[Authorize(Permissions.ManageInstruments)]
[Authorize(Policies.ServiceEngineerOrRndMode)]
public class PairingHub(
    IHubContext<PairingHub, IInstrumentPairingHub> hubContext,
    ILogger<PairingHub> logger) : HubBase<PairingHub, IInstrumentPairingHub>(hubContext, logger), IInstrumentPairingHub
{
    protected override string DefaultGroup => "pairing_default";
    public Task PairingModeStateChanged(bool enabled)
    {
        return HubContext.Clients.Group(DefaultGroup).PairingModeStateChanged(enabled);
    }

    public Task ApprovalRequested(string instrumentNumber)
    {
        return HubContext.Clients.Group(DefaultGroup).ApprovalRequested(instrumentNumber);
    }

    public Task InstrumentPaired(string instrumentNumber)
    {
        return HubContext.Clients.Group(DefaultGroup).InstrumentPaired(instrumentNumber);
    }
}
