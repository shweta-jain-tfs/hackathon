namespace Pharos.Contracts.Notifications;

/// <summary>
/// The strongly typed server-to-client RPC interface for instrument notifications.
/// </summary>
public interface IInstrumentHub : IEntityHub
{
    Task AssaySchedulerChanged(int instrumentId, int assaySchedulerId, bool assaySchedulerIsActive, DateTimeOffset? assaySchedulerPausedUntilDate);
    Task AssaySchedulerDayChanged(int instrumentId, int assaySchedulerId, AssaySchedulerDayDto assaySchedulerDay, bool anyDayHasTime);
    Task AssaySchedulerNextRunChanged(int instrumentId, DateTimeOffset? nextRun, int nextRunType);
    Task InstrumentStateChanged(int instrumentId, int instrumentState);
    Task TimeWhenNextStateReachedChanged(int instrumentId, DateTimeOffset? timeWhenNextStateReached);
    Task InstrumentTechnologiesChanged(int instrumentId);
    Task NumberOfTestsUpdated(int instrumentId, int numberOfTests);
    Task SignalTowerStateChanged(int instrumentId, int signalTowerState);
}
