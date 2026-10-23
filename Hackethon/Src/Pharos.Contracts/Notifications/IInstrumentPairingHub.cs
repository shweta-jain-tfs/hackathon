namespace Pharos.Contracts.Notifications;

public interface IInstrumentPairingHub : IHub
{
    Task PairingModeStateChanged(bool enabled);
    Task ApprovalRequested(string instrumentNumber);
    Task InstrumentPaired(string instrumentNumber);
}
