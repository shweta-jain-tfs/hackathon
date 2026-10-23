namespace Pharos.Common.Web.Components.Reagent;

/// <summary>
/// Represents the status of bottles for use in the bottle legend.
/// Only includes statuses that should be displayed in the legend (excludes Available and Reserved).
/// </summary>
public enum BottleStatus
{
    /// <summary>
    /// Bottle is currently in use
    /// </summary>
    InUse = 1,
    
    /// <summary>
    /// Bottle is empty
    /// </summary>
    Empty = 2
}

public enum BottleError
{
    CloseDrawer = 1
}
