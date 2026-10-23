namespace Pharos.Common.Web.Components.Menu;

internal record SubMenuRegistration(string MatchPrefix);

internal sealed class MenuContext
{
    public string? ActiveRouteSubMenuPrefix { get; internal set; }
    public bool IsRouteSubMenuOpen { get; internal set; }
    public bool HasActiveRouteSubMenu => ActiveRouteSubMenuPrefix is not null;
    /// <summary>
    /// Incremented each time the active route submenu item should scroll into view.
    /// Route submenu components use this to avoid reacting to unrelated re-renders.
    /// </summary>
    public int ScrollRevision { get; internal set; }

    public Action<SubMenuRegistration>? RegisterRouteSubMenu { get; init; }

    /// <summary>
    /// Toggles the active route submenu using its MatchPrefix.
    /// </summary>
    public Action<string>? ToggleRouteSubMenu { get; init; }
}
