namespace Pharos.Server.Reagents.Application.Queries;

/// <summary>
/// DTO for solid waste drawer with 2 predefined container positions
/// </summary>
public class SolidWasteDrawerDetailsDto
{
    /// <summary>
    /// Drawer lock state
    /// </summary>
    public LockState LockState { get; set; }

    /// <summary>
    /// Drawer unlock in progress - Pharos internal state
    /// </summary>
    public bool IsUnlockInProgress { get; set; }

    /// <summary>
    /// All waste containers: Biohazard (position 1) and Recyclable (position 2)
    /// Always contains exactly 2 containers
    /// </summary>
    public List<WasteContainerDto> Containers { get; set; } = new();
}

/// <summary>
/// Waste container DTO representing a single solid waste bin
/// </summary>
public class WasteContainerDto
{
    /// <summary>
    /// Container position (1 for Biohazard, 2 for Recyclable)
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Type of waste (Biohazard or Recyclable)
    /// </summary>
    public WasteType Type { get; set; }
    
    /// <summary>
    /// Fill percentage (0-100)
    /// </summary>
    public int Percentage { get; set; }
    
    /// <summary>
    /// Container status
    /// </summary>
    public ReagentPositionStatus Status { get; set; }

    /// <summary>
    /// Is Container Empty
    /// </summary>
    public bool IsEmpty { get; set; }

    /// <summary>
    /// Is Container Present
    /// </summary>
    public bool IsPresent { get; set; }

    /// <summary>
    /// Empty indication type for this waste container
    /// Blue (Pending): >50% full
    /// Yellow (Warning): TimeCapDispenseStop within 1 hour
    /// Red (Alarm): CapDispenseStopped
    /// </summary>
    public FillOrEmptyIndicationType EmptyIndicationType { get; set; }
}

/// <summary>
/// Types of solid waste containers
/// </summary>
public enum WasteType
{
    /// <summary>
    /// Biohazard waste
    /// </summary>
    Biohazard = 1,

    /// <summary>
    /// Recyclable waste
    /// </summary>
    Recyclable = 2
}
