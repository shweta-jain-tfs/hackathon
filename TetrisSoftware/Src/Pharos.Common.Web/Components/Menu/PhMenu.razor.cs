using Microsoft.AspNetCore.Components.Routing;

namespace Pharos.Common.Web.Components.Menu;

/// <summary>
/// Navigation menu with optional contextual route submenus.
/// </summary>
public partial class PhMenu : IAsyncDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Optional <see cref="PhRouteSubMenu"/> sub menu definitions
    /// </summary>
    [Parameter] public RenderFragment? RouteSubMenus { get; set; }

    /// <summary>
    /// Overrides the current relative URI used for active route matching.
    /// Intended for testing and Storybook scenarios.
    /// </summary>
    [Parameter] public string? CurrentUriOverride { get; set; }

    private readonly Dictionary<string, SubMenuRegistration> _routeSubMenus = new(StringComparer.OrdinalIgnoreCase);
    private MenuContext _menuContext = default!;
    private bool _shouldMatchActiveSubMenu = true;

    protected override void OnInitialized()
    {
        _menuContext = new MenuContext
        {
            RegisterRouteSubMenu = RegisterRouteSubMenu,
            ToggleRouteSubMenu = ToggleRouteSubMenu,
        };

        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    protected override void OnParametersSet()
    {
        if (CurrentUriOverride is not null)
        {
            MatchActiveSubMenu(CurrentUriOverride);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (_shouldMatchActiveSubMenu)
        {
            _shouldMatchActiveSubMenu = false;
            MatchActiveSubMenu(CurrentUriOverride ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri));
            if (_menuContext.HasActiveRouteSubMenu)
            {
                StateHasChanged();
            }
        }
    }

    private void RegisterRouteSubMenu(SubMenuRegistration registration)
    {
        _routeSubMenus[NormalizePath(registration.MatchPrefix)] = registration;
        // Re-evaluate the active submenu after registration changes.
        _shouldMatchActiveSubMenu = true;
    }

    private void ToggleRouteSubMenu(string subMenuPrefix)
    {
        if (_menuContext.ActiveRouteSubMenuPrefix != subMenuPrefix)
        {
            return;
        }

        _menuContext.IsRouteSubMenuOpen = !_menuContext.IsRouteSubMenuOpen;
        StateHasChanged(); // to set inert flag
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (CurrentUriOverride is not null || _routeSubMenus.Count == 0)
        {
            return;
        }

        var path = NavigationManager.ToBaseRelativePath(e.Location);
        MatchActiveSubMenu(path);
        _ = InvokeAsync(StateHasChanged);
    }

    private void MatchActiveSubMenu(string path)
    {
        var match = _routeSubMenus.Values
            .OrderByDescending(x => x.MatchPrefix.Length)
            .FirstOrDefault(x => IsPrefixMatch(path, x.MatchPrefix));
        _menuContext.ActiveRouteSubMenuPrefix = match?.MatchPrefix;
        _menuContext.IsRouteSubMenuOpen = match is not null; // reopen on every matching navigation
        if (match is not null)
        {
            _menuContext.ScrollRevision++;
        }
    }

    private static bool IsPrefixMatch(string currentPath, string prefix)
    {
        var current = NormalizePath(currentPath);
        var p = NormalizePath(prefix);

        if (p.Length == 0)
        {
            return current.Length == 0; // root only matches root
        }

        return current.Equals(p, StringComparison.OrdinalIgnoreCase)
            || current.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return path.Trim('/');
    }

    public ValueTask DisposeAsync()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}