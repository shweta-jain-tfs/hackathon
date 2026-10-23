namespace Pharos.Server.Domain.Enums;


/// <summary>
/// List all available instrument operational status.
/// </summary>
public enum InstrumentState
{
    /// <summary>
    /// The default state of the instrument when it is not connected to the server.
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Pharos and the instrument not fully compatible, only file transfer and software update is allowed, no other communication is possible.
    /// </summary>
    Limited,

    /// <summary>
    /// The instrument has started a communication session with the Pharos, but the real instrument state is not known yet. The state synchronization is in progress.
    /// </summary>
    Synchronizing,

    //Below are the states from EnumInstrumentState part of protocol
    Aborting,
    Assay,
    AssayCmr,
    AssayCmrFinished,
    AssayFinishing,
    AssayFinishOrdered,
    BlankMeasurement,
    CmrPrepare,
    MaintenanceRinsing,
    MaintenanceExtendedRinsing,
    MaintenanceCleaning,
    MaintenanceCleaningHypochlorite,
    MaintenanceManualCleaning,
    MaintenanceBlankMeasurement,
    PowerSaving,
    Priming,
    Rinsing,
    Shutdown,
    Service,
    ServiceVerifyingStorage,
    ServiceEjectingCapWell,
    Standby,
    Startup
}