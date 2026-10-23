using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Pharos;
using Tetris.Protocol.Pharos.ProcessStatus;
using InstrumentState = Pharos.Server.Domain.Enums.InstrumentState;

namespace Pharos.Server.Instrument.Domain;

/// <summary>
/// Represents an instrument installed in a lab.
/// This class includes only persistent properties, the instrument operational state (which is transient) is handled separately.
/// </summary>
internal class Instrument : BaseAuditableEntity, IInstrument
{
    private InstrumentEnvironment _environment = new();

    /// <summary>
    /// Gets or sets a human-readable name of the instrument. Typically, it is set by service engineers at the time of system setup.
    /// </summary>
    [MaxLength(Constants.InstrumentDisplayNameLength)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Possibility to disable an instrument.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the instrument type.
    /// </summary>
    public InstrumentType InstrumentType { get; set; }

    /// <summary>
    /// Gets or sets the instrument serial number. Pharos will use it to authenticate instruments when they connect to the server.
    /// </summary>
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string SerialNumber { get; set; } = null!;

    /// <summary>
    /// Instrument software version reported when the communication session started.
    /// </summary>
    [MaxLength(Constants.InstrumentSoftwareVersionLength)]
    public string? InstrumentSoftwareVersion { get; set; }


    /// <summary>
    /// Gets or set the instrument IP address.
    /// </summary>
    public IPAddress IpAddress { get; set; } = null!;

    [NotMapped]
    public DateTimeOffset? InstallationDate { get; set; }

    /// <summary>
    /// Gets or sets the instrument operational status (operational state).
    /// </summary>
    [NotMapped]
    public InstrumentState State { get; set; }

    [NotMapped]
    public DateTimeOffset? TimeWhenNextStateReached { get; set; }

    [NotMapped]
    public bool Paused { get; set; }

    [NotMapped]
    public bool FatalError { get; set; }

    [NotMapped]
    public ProcessStatus? ProcessStatus { get; set; }

    [NotMapped]
    public IDrawerLockStateList DrawersLockState { get; set; } = CreateDefaultDrawerLockStates();

    [NotMapped]
    public IReadOnlyList<ContinuousSensor> EnvironmentSensors => _environment.Sensors;

    [NotMapped]
    public Dictionary<DrawerType, bool> DrawersScanState { get; } = new()
    {
        { DrawerType.Sample1, false },
        { DrawerType.Sample2, false },
        { DrawerType.Reagent, false },
    };


    [NotMapped]
    public HoodLockState HoodLockState { get; set; } = new(LockState.ClosedLocked, false);

    [NotMapped]
    public SignalTower SignalTower { get; set; } = new();

    [NotMapped]
    public IReadOnlyList<HardwareModule>? Modules { get; set; }

    [NotMapped]
    public bool MaintenanceNeeded { get; set; }

    [NotMapped]
    public Guid AssayRunId { get; set; }

    public enum ProcessStatusTimeSource
    {
        None,
        LastRemainingResultTime,
        LastRemainingStartTime
    }

    /// <summary>
    /// Checks if the current state is valid for sending a process status time message and returns the appropriate time source.
    /// </summary>
    /// <remarks>
    /// Process status messages provide timing updates when the instrument is actively executing assay operations.
    /// Time is derived from remaining result times when waiting for assay completion, or from start times when finishing.
    /// </remarks>
    /// <returns>A tuple with IsValid flag and the TimeSource to use if valid.</returns>
    public (bool IsValid, ProcessStatusTimeSource TimeSource) IsValidStateForProcessStatusTimeMessage()
    {
        return State switch
        {
            // Use remaining result time while assay is in progress.
            InstrumentState.Assay
                => (true, ProcessStatusTimeSource.LastRemainingResultTime),

            // Use remaining start time when assay is being ordered to finish.
            InstrumentState.AssayFinishOrdered
                => (true, ProcessStatusTimeSource.LastRemainingStartTime),

            // All other states do not support process status timing.
            _ => (false, ProcessStatusTimeSource.None)
        };
    }

    /// <summary>
    /// Checks whether a state supports publishing the next state reached time from instrument state messages.
    /// </summary>
    /// <param name="state">The instrument state to validate.</param>
    /// <returns><see langword="true"/> when publishing the next state reached time is allowed for the provided state; otherwise <see langword="false"/>.</returns>
    public static bool IsValidStateForStateTimeMessage(InstrumentState state)
    {
        return state
            is InstrumentState.Aborting
            or InstrumentState.AssayCmr
            or InstrumentState.AssayFinishing
            or InstrumentState.BlankMeasurement
            or InstrumentState.MaintenanceBlankMeasurement
            or InstrumentState.MaintenanceCleaning
            or InstrumentState.MaintenanceExtendedRinsing
            or InstrumentState.MaintenanceManualCleaning
            or InstrumentState.MaintenanceRinsing
            or InstrumentState.Priming
            or InstrumentState.Rinsing
            or InstrumentState.ServiceEjectingCapWell
            or InstrumentState.ServiceVerifyingStorage
            or InstrumentState.Shutdown
            or InstrumentState.Startup;
    }

    /// <summary>
    /// Checks if the instrument is in assay mode.
    /// </summary>
    /// <returns></returns>
    public bool IsInAssayMode
    {
        get
        {
            //ToDo -- need to check if below states are sufficient to determine that instrument is in assay mode
            var validStates = new[]
            {
                InstrumentState.Assay,
                InstrumentState.AssayCmr,
                InstrumentState.AssayFinishing,
                InstrumentState.AssayFinishOrdered,
                InstrumentState.Priming,
                InstrumentState.BlankMeasurement
            };

            return validStates.Contains(State);
        }
    }

    public bool CanStartMaintenance
    {
        get
        {
            var validStates = new[]
            {
                InstrumentState.Standby,
                InstrumentState.PowerSaving
            };

            return validStates.Contains(State);
        }
    }

    public bool IsInMaintenanceMode
    {
        get
        {
            var validStates = new[]
            {
                InstrumentState.MaintenanceCleaning,
                InstrumentState.MaintenanceExtendedRinsing,
                InstrumentState.MaintenanceManualCleaning,
                InstrumentState.MaintenanceRinsing
            };

            return validStates.Contains(State);
        }
    }

    public void ResetState()
    {
        DrawersLockState = CreateDefaultDrawerLockStates();
        HoodLockState = new HoodLockState(LockState.ClosedLocked, false);
        _environment = new InstrumentEnvironment();
        TimeWhenNextStateReached = null;
        ProcessStatus = null;
        State = InstrumentState.Offline;
    }

    internal static DrawerLockStateList CreateDefaultDrawerLockStates()
    {
        var result = new DrawerLockStateList();
        foreach (var drawerType in Enum.GetValues<DrawerType>())
        {
            result.Add(new DrawerLockState(drawerType, drawerType is DrawerType.RinseWaste or DrawerType.BottledReagent ? LockState.ClosedUnlocked : LockState.ClosedLocked, false));
        }

        return result;
    }
}

internal class DrawerLockStateList : List<DrawerLockState>, IDrawerLockStateList
{
    public DrawerLockState this[DrawerType drawerType] => this.First(x => x.Drawer == drawerType);
}

internal record HoodLockState(LockState LockState, bool IsUnlockInProgress);