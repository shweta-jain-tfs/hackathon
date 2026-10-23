namespace Pharos.Common.Web.Components.Reagent;

/// <summary>
/// Represents the status of liquid bottles for use in the liquid legend.
/// Only includes statuses that should be displayed in the legend.
/// </summary>
public enum LiquidBottleStatus
{
    /// <summary>
    /// Liquid bottle is disabled (piped out/in but not in use)
    /// </summary>
    Disabled,

    /// <summary>
    /// Liquid bottle is currently in use
    /// </summary>
    InUse
}

public enum LiquidError
{
    CloseDrawer = 1
}
