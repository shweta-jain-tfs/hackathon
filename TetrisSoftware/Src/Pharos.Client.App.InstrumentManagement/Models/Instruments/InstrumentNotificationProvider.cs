using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.InstrumentManagement.Models.Instruments;

internal sealed class InstrumentNotificationProvider(
    IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), IInstrumentNotificationProvider
{
    public event Action<int, int, bool, DateTimeOffset?> AssaySchedulerChanged
    {
        add => Subscribe(nameof(AssaySchedulerChanged), value);
        remove => Unsubscribe(nameof(AssaySchedulerChanged), value);
    }

    public event Action<int, int, AssaySchedulerDay, bool> AssaySchedulerDayChanged
    {
        add => Subscribe(nameof(AssaySchedulerDayChanged), value);
        remove => Unsubscribe(nameof(AssaySchedulerDayChanged), value);
    }

    public event Action<int, DateTimeOffset?, AssaySchedulerNextRunType> AssaySchedulerNextRunChanged
    {
        add => Subscribe(nameof(AssaySchedulerNextRunChanged), value);
        remove => Unsubscribe(nameof(AssaySchedulerNextRunChanged), value);
    }

    public event Action<int, InstrumentState> InstrumentStateChanged
    {
        add => Subscribe(nameof(InstrumentStateChanged), value);
        remove => Unsubscribe(nameof(InstrumentStateChanged), value);
    }

    public event Action<int, DateTimeOffset?> TimeWhenNextStateReachedChanged
    {
        add => Subscribe(nameof(TimeWhenNextStateReachedChanged), value);
        remove => Unsubscribe(nameof(TimeWhenNextStateReachedChanged), value);
    }

    public event Action<int> InstrumentTechnologiesChanged
    {
        add => Subscribe(nameof(InstrumentTechnologiesChanged), value);
        remove => Unsubscribe(nameof(InstrumentTechnologiesChanged), value);
    }

    public event EventHandler NumberOfTestsUpdated
    {
        add => Subscribe(nameof(NumberOfTestsUpdated), value);
        remove => Unsubscribe(nameof(NumberOfTestsUpdated), value);
    }

    public event EventHandler SignalTowerStateChanged
    {
        add => Subscribe(nameof(SignalTowerStateChanged), value);
        remove => Unsubscribe(nameof(SignalTowerStateChanged), value);
    }

    protected override string HubRoute => HubRoutes.Instrument;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<int, int, bool, DateTimeOffset?>(nameof(IInstrumentHub.AssaySchedulerChanged), OnAssaySchedulerChanged);
        hubConnection.On<int, int, AssaySchedulerDayDto, bool>(nameof(IInstrumentHub.AssaySchedulerDayChanged), OnAssaySchedulerDayChanged);
        hubConnection.On<int, DateTimeOffset?, int>(nameof(IInstrumentHub.AssaySchedulerNextRunChanged), OnAssaySchedulerNextRunChanged);
        hubConnection.On<int, InstrumentState>(nameof(IInstrumentHub.InstrumentStateChanged), OnInstrumentStateChanged);
        hubConnection.On<int, DateTimeOffset?>(nameof(IInstrumentHub.TimeWhenNextStateReachedChanged), OnTimeWhenNextStateReachedChanged);
        hubConnection.On<int>(nameof(IInstrumentHub.InstrumentTechnologiesChanged), OnInstrumentTechnologiesChanged);
        hubConnection.On<int, int>(nameof(IInstrumentHub.NumberOfTestsUpdated), OnNumberOfTestsUpdated);
        hubConnection.On<int, int>(nameof(IInstrumentHub.SignalTowerStateChanged), OnSignalTowerStateChanged);
    }

    private void OnAssaySchedulerChanged(int instrumentId, int assaySchedulerId, bool assaySchedulerIsActive, DateTimeOffset? assaySchedulerPausedUntilDate)
    {
        GetHandler<Action<int, int, bool, DateTimeOffset?>>(nameof(AssaySchedulerChanged))?.Invoke(instrumentId, assaySchedulerId, assaySchedulerIsActive, assaySchedulerPausedUntilDate);
    }

    private void OnAssaySchedulerDayChanged(int instrumentId, int assaySchedulerId, AssaySchedulerDayDto assaySchedulerDayDto, bool anyDayHasTime)
    {
        var assaySchedulerDay = assaySchedulerDayDto.ToAssaySchedulerDay();
        GetHandler<Action<int, int, AssaySchedulerDay, bool>>(nameof(AssaySchedulerDayChanged))?.Invoke(instrumentId, assaySchedulerId, assaySchedulerDay, anyDayHasTime);
    }

    private void OnAssaySchedulerNextRunChanged(int instrumentId, DateTimeOffset? nextRun, int nextRunType)
    {
        // Get an int and cast it to AssaySchedulerNextRunType.
        GetHandler<Action<int, DateTimeOffset?, AssaySchedulerNextRunType>>(nameof(AssaySchedulerNextRunChanged))?.Invoke(instrumentId, nextRun, (AssaySchedulerNextRunType)nextRunType);
    }

    private void OnInstrumentStateChanged(int instrumentId, InstrumentState status)
    {
        GetHandler<Action<int, InstrumentState>>(nameof(InstrumentStateChanged))?.Invoke(instrumentId, status);
    }

    private void OnTimeWhenNextStateReachedChanged(int instrumentId, DateTimeOffset? timeWhenNextStateReached)
    {
        GetHandler<Action<int, DateTimeOffset?>>(nameof(TimeWhenNextStateReachedChanged))?.Invoke(instrumentId, timeWhenNextStateReached);
    }

    private void OnInstrumentTechnologiesChanged(int instrumentId)
    {
        GetHandler<Action<int>>(nameof(InstrumentTechnologiesChanged))?.Invoke(instrumentId);
    }

    private void OnNumberOfTestsUpdated(int instrumentId, int numberOfTests)
    {
        GetHandler<EventHandler>(nameof(NumberOfTestsUpdated))?.Invoke(this, EventArgs.Empty);
    }

    private void OnSignalTowerStateChanged(int instrumentId, int signalTowerState)
    {
        GetHandler<EventHandler>(nameof(SignalTowerStateChanged))?.Invoke(this, EventArgs.Empty);
    }
}
