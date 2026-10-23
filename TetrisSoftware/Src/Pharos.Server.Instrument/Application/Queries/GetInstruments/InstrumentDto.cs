#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
// ReSharper disable UnusedMember.Global

using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Queries.GetInstruments;

/// <inheritdoc cref="Instrument"/>
public class InstrumentDto
{
    public int Id { get; set; }

    public string DisplayName { get; set; }

    public bool Enabled { get; set; }
    
    public string SerialNumber { get; set; }

    public InstrumentState State { get; set; }

    public HoodLockStateDto HoodLockState { get; set; } = new();

    public DateTimeOffset? TimeWhenNextStateReached { get; set; }

    public int NumberOfTests { get; set; }

    public SignalTowerDto SignalTower { get; set; } = new();

    public bool AssaySchedulerIsActive { get; set; }
    public DateTimeOffset? AssaySchedulerIsPausedUntilDate { get; set; }
    public AssaySchedulerNextRunType AssaySchedulerNextRunType { get; set; }
    public DateTimeOffset? AssaySchedulerNextRun { get; set; }
    public bool AssaySchedulerShowInfo { get; set; }
    public bool MaintenanceNeeded { get; set; }
    public List<AssayTechnology> SelectedTechnologies { get; set; } = [];
}

public class HoodLockStateDto {
    public LockState LockState { get; set; }
    public bool IsUnlockInProgress { get; set; }
}

public enum AssaySchedulerNextRunType
{
    None = 0,
    Start = 1,
    End = 2
}
