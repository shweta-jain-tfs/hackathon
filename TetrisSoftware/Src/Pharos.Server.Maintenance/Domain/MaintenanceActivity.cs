using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Maintenance.Domain;

public class MaintenanceActivity : BaseEntity
{
    public MaintenanceActivityType ActivityType { get; set; }
    public MaintenanceScheduledType ScheduledType { get; set; }
    public int Duration { get; set; }
    public DateTimeOffset LastCompleted { get; set; }

    public DateTimeOffset? Next { get; set; }

    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string InstrumentNumber { get; set; } = string.Empty;

    public Guid MaintenanceId { get; set; }
}

public enum MaintenanceScheduledType
{
    OnDemand,
    Scheduled,
    Both
}

public enum MaintenanceActivityType
{
    Cleaning, //Scheduled
    ManualCleaning, //Scheduled
    ExtendedRinse,
    Rinse,
    PipetteCleaning,
    CleanWasteChute,
    CleanLiquidWasteContainer,
    CleanRinseContainer,
    CleanSolidWasteBins,
    CleanWashStations,
    CleanTransferUnitTips,
    CleanEjectUnitTips,
    InspectDispenseUnit,
    InspectCapWellRemovers
}

public enum MaintenanceActivityStatus
{
    None,
    DueLater,
    DueSoon,
    Overdue,
}