namespace Pharos.Server.Reagents.Application.Queries;

/// <summary>
/// DTO for liquid drawer with predefined container positions
/// Maps from InstrumentStorage.LiquidWaste and RinseContainers protocol messages
/// Always contains 4 containers: Waste positions 1,2 then Rinse positions 1,2
/// </summary>
public class LiquidDrawerDetailsDto
{
    /// <summary>
    /// Drawer lock state
    /// </summary>
    public LockState LockState { get; set; }
    /// <summary>
    /// All liquid containers in predefined order: Waste 1, Waste 2, Rinse 1, Rinse 2
    /// Always contains exactly 4 containers regardless of protocol data
    /// </summary>
    public List<LiquidContainer> Containers { get; set; } = new();
}

/// <summary>
/// Liquid container DTO representing a single bottle position
/// Maps from WasteContainer or RinseContainer protocol messages
/// </summary>
public class LiquidContainer
{
    /// <summary>
    /// Container position (1 or 2)
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Container type (Waste or Rinse)
    /// </summary>
    public LiquidContainerType Type { get; set; }
    
    /// <summary>
    /// Currently active container (InUse from protocol)
    /// </summary>
    public bool InUse { get; set; }
    
    /// <summary>
    /// Fill percentage (0-100) from protocol data
    /// </summary>
    public int Percentage { get; set; }
    
    /// <summary>
    /// Container status mapped to ReagentPositionStatus
    /// </summary>
    public ReagentPositionStatus Status { get; set; }
    
    /// <summary>
    /// True when piped functionality is enabled for this container type
    /// Waste: based on LiquidSettings.Waste.PipedOut
    /// Rinse: based on LiquidSettings.Rinse.PipedIn
    /// </summary>
    public bool IsPipedEnabled { get; set; }
    
    /// <summary>
    /// True when no bottle is present at this position (not in protocol data)
    /// </summary>
    public bool IsMissing { get; set; }

    /// <summary>
    /// Fill or Empty notification type for this container
    /// </summary>
    public FillOrEmptyIndicationType FillOrEmpty { get; set; }
}

/// <summary>
/// Container types for liquid drawer
/// </summary>
public enum LiquidContainerType
{
    Waste = 1,
    Rinse = 2
}

/// <summary>
/// Warning types for liquid containers
/// </summary>
public enum FillOrEmptyIndicationType
{
    None,
    Pending,
    Warning,
    Alarm
}
