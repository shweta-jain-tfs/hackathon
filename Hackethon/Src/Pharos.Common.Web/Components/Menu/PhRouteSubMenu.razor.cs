using Microsoft.JSInterop;
using Pharos.Common.Web.Extensions;

namespace Pharos.Common.Web.Components.Menu;

/// <summary>
/// Contextual submenu for <see cref="PhMenu.RouteSubMenus"/>.
/// Displayed by <see cref="PhMenu"/> when the current route matches <see cref="MatchPrefix"/>.
/// </summary>
public partial class PhRouteSubMenu
{
    [CascadingParameter] internal MenuContext? MenuContext { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The title of the sub menu.
    /// </summary>
    [Parameter] public required string Title { get; set; }

    /// <summary>
    /// Prefix used to match routes for this sub menu (for example, "settings" matches
    /// "settings" and "settings/sub").
    /// </summary>
    /// <remarks>
    /// Immutable after initialization.
    /// </remarks>
    [Parameter] public required string MatchPrefix { get; set; }

    private string _registeredMatchPrefix = default!;
    private ElementReference _openToggleRef;
    private ElementReference _closeToggleRef;
    // Set only on explicit user toggle; route-driven open/close should not move focus.
    private bool _moveFocusAfterRender;
    private IJSObjectReference? _script;
    private int? _lastScrollRevision;

    private bool IsActive => MenuContext?.ActiveRouteSubMenuPrefix == MatchPrefix;
    private bool IsOpen => IsActive && MenuContext?.IsRouteSubMenuOpen == true;

    protected override void OnInitialized()
    {
        _registeredMatchPrefix = MatchPrefix;

        MenuContext?.RegisterRouteSubMenu?.Invoke(new SubMenuRegistration(MatchPrefix));
    }

    protected override void OnParametersSet()
    {
        if (MatchPrefix != _registeredMatchPrefix)
        {
            throw new InvalidOperationException($"{nameof(PhRouteSubMenu)} does not support changing {nameof(MatchPrefix)} after initialization.");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _script = await ImportCommonUtilityJSAsync("scroll.js");
        }

        if (_moveFocusAfterRender)
        {
            _moveFocusAfterRender = false;

            if (IsOpen)
            {
                await _closeToggleRef.FocusAsync();
            }
            else
            {
                await _openToggleRef.FocusAsync();
            }
        }

        if (IsActive)
        {
            var scrollRevision = MenuContext?.ScrollRevision;

            if (_lastScrollRevision != scrollRevision)
            {
                _lastScrollRevision = scrollRevision;

                if (_script is not null)
                {
                    // Wait for menu items to react to the route change before scrolling.
                    await Task.Yield();

                    await _script.InvokeVoidAsync(
                        "scrollSelectorIntoView",
                        ".ph-submenu-panel [aria-current='page']");
                }
            }
        }
    }

    private void ToggleOpen()
    {
        // Move focus to the visible toggle after open/close so keyboard users can toggle back and forth.
        _moveFocusAfterRender = true;
        MenuContext?.ToggleRouteSubMenu?.Invoke(MatchPrefix);
    }

    public override async ValueTask DisposeAsync()
    {
        await _script.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }
}