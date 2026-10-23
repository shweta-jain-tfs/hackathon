namespace Pharos.Server.Reagents.Application.Queries;

public class ReagentDrawerDto
{
    public LockState LockState { get; set; }
    public bool IsUnlockInProgress { get; set; }
    public bool IsScanOngoing { get; set; }
    public bool NeedEmptyLoader { get; set; }

    /// <summary>
    /// List of reagent loader positions in the reagent drawer.
    /// </summary>
    public List<ReagentLoaderDto> ReagentLoaders { get; set; } = default!;

    /// <summary>
    /// List of dilution plate stack positions in the reagent drawer.
    /// </summary>
    public List<DilutionPlateStackDto> DilutionPlateStacks { get; set; } = default!;
}

public abstract class DrawerItemBaseDto
{
    /// <summary>
    /// Position of the item in the drawer.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Indicates if the drawer item is empty.
    /// </summary>
    public bool IsEmpty { get; set; }
}

public class DilutionPlateStackDto : DrawerItemBaseDto
{
    /// <summary>
    /// Number of plates in the dilution plate stack.
    /// </summary>
    public int NumberOfPlates { get; set; }

    /// <summary>
    /// Status of the reagent position.
    /// </summary>
    public ReagentPositionStatus Status { get; set; }
}

public class ReagentLoaderDto : DrawerItemBaseDto
{
    /// <summary>
    /// List of compartments in the reagent loader.
    /// </summary>
    public List<ReagentCompartmentDto> Compartments { get; set; } = default!;

    public string Location { get; set; } = string.Empty;

    /// <summary>
    ///  Status of loader
    /// </summary>
    public ReagentPositionStatus Status { get; set; }
}

public class ReagentCompartmentDto : DrawerItemBaseDto
{
    /// <summary>
    /// Type of the compartment (Cartridge or Carrier).
    /// </summary>
    public CompartmentReagentType Type { get; set; }

    /// <summary>
    /// List of compartments in the reagent loader.
    /// </summary>
    public List<CompartmentCellContentDto> CellContent { get; set; } = default!;
}

public class CompartmentCellContentDto : DrawerItemBaseDto
{
    public int CompartmentPosition { get; set; }
    public string ReagentBarcode { get; set; } = default!;
    
    /// <summary>
    /// Status of the reagent position.
    /// </summary>
    public ReagentPositionStatus Status { get; set; }

    /// <summary>
    /// Indicates that an unload was requested for this position.
    /// This is independent from the position status, including error statuses.
    /// </summary>
    public bool IsUnloadRequested { get; set; }
}

public enum CompartmentReagentType
{
    Carrier,
    Cartridge
}
