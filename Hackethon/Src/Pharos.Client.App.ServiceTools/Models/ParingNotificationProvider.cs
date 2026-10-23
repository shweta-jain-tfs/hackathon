using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.Models.Services;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.ServiceTools.Models;

public class PairingNotificationProvider : HubNotificationProvider
{
    public PairingNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }
    public event Func<bool, Task>? PairingModeStateChanged
    {
        add => Subscribe(nameof(PairingModeStateChanged), value);
        remove => Unsubscribe(nameof(PairingModeStateChanged), value);
    }

    public event Func<string, Task>? ApprovalRequested
    {
        add => Subscribe(nameof(ApprovalRequested), value);
        remove => Unsubscribe(nameof(ApprovalRequested), value);
    }

    public event Func<string, Task>? InstrumentPaired
    {
        add => Subscribe(nameof(InstrumentPaired), value);
        remove => Unsubscribe(nameof(InstrumentPaired), value);
    }

    protected override string HubRoute => HubRoutes.Pairing;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<bool>(nameof(IInstrumentPairingHub.PairingModeStateChanged), OnPairingModeStateChanged);
        hubConnection.On<string>(nameof(IInstrumentPairingHub.ApprovalRequested), OnApprovalRequested);
        hubConnection.On<string>(nameof(IInstrumentPairingHub.InstrumentPaired), OnInstrumentPaired);
    }

    private Task OnPairingModeStateChanged(bool enabled)
    {
        return GetHandler<Func<bool, Task>>(nameof(PairingModeStateChanged))?.Invoke(enabled) ?? Task.CompletedTask;
    }

    private Task OnApprovalRequested(string instrumentNumber)
    {
        return GetHandler<Func<string, Task>>(nameof(ApprovalRequested))?.Invoke(instrumentNumber) ?? Task.CompletedTask;
    }

    private Task OnInstrumentPaired(string instrumentNumber)
    {
        return GetHandler<Func<string, Task>>(nameof(InstrumentPaired))?.Invoke(instrumentNumber) ?? Task.CompletedTask;
    }
}