using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Interfaces;

[Flags]
internal enum CommunicationSynchronizationState
{
    NotStarted = 0,
    UpdateLimitsSent = 1,
    UpdateMethodsSent = 2,
    UpdateArticlesSent = 4,
    ActiveMethodsSent = 8,
    QcSchedulingSent = 16,
    ProcessStatusReceived = 32,
    SensorInformationReceived = 64,
    InstrumentStorageReceived = 128,
    CurrentErrorListReceived = 256,
    SignalTowerUpdated = 512,
    CalibrationInfoSent = 1024,
    InstrumentStateReceived = 2048,
    CurrentMaintenanceInfoReceived = 4096,
    MissingConsumablesReceived = 8192,
    UpdateMethodStatusSent = 16384,
    UpdateSettingsSent = 32768,

    Synchronized = UpdateLimitsSent | UpdateMethodsSent | UpdateArticlesSent | ActiveMethodsSent
                   | QcSchedulingSent | ProcessStatusReceived
                   | SensorInformationReceived | InstrumentStorageReceived | CurrentErrorListReceived
                   | SignalTowerUpdated | CalibrationInfoSent | InstrumentStateReceived
                   | CurrentMaintenanceInfoReceived | MissingConsumablesReceived
                   | UpdateMethodStatusSent | UpdateSettingsSent
}

/// <summary>
/// Contains data related to an instrument communication session.
/// </summary>
internal class CommunicationSession
{
    public const string SessionIdHeader = "sessionId";
    public const string InstrumentSerialHeader = "instrumentSerial";

    private readonly Lock _lock = new();
    private CommunicationSynchronizationState _synchronizationState;

    public Guid SessionId { get; init; }
    public string InstrumentNumber { get; init; } = null!;
    public IInstrumentConnection Connection { get; init; } = null!;
    public EnumCompatibilityStatus CompatibilityStatus { get; init; } = EnumCompatibilityStatus.FullyCompatible;

    public bool IsSynchronized
    {
        get
        {
            lock (_lock)
            {
                return _synchronizationState == CommunicationSynchronizationState.Synchronized;
            }

        }
    }

    public bool SetSynchronizationFlag(CommunicationSynchronizationState flag)
    {
        lock (_lock)
        {
            if (_synchronizationState != CommunicationSynchronizationState.Synchronized)
            {
                // The session is not yet synchronized 
                _synchronizationState |= flag;
                if (_synchronizationState == CommunicationSynchronizationState.Synchronized)
                {
                    // Now the session is synchronized
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Temporary holds the actual instrument state during synchronization.
    /// When the instrument and Pharos are synchronizing, we want to display Synchronizing
    /// even if we receive the actual state from the instrument.
    /// </summary>
    public InstrumentState? CachedInstrumentState { get; set; }
}