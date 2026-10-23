using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Icons;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// A navigation arrow button used in the sidebar for back/forward history navigation.
/// Renders a button with chevron icon pointing in the navigation direction.
/// </summary>
public partial class PhNavigationButton
{
    /// <summary>
    /// Disables the button and applies disabled styling.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Specifies which navigation button to display (left or right).
    /// </summary>
    [Parameter]
    public NavigationButtonType ButtonType { get; set; } = NavigationButtonType.Left;

    /// <summary>
    /// Raised when the button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    protected IconType IconType { get; set; } = IconType.ChevronLeft;
    protected string ButtonClass { get; set; } = "ph-navigation-button--left";

    protected override void OnParametersSet()
    {
        switch (ButtonType)
        {
            case NavigationButtonType.Left:
                IconType = IconType.ChevronLeft;
                ButtonClass = "ph-navigation-button--left";
                break;
            case NavigationButtonType.Right:
                IconType = IconType.ChevronRight;
                ButtonClass = "ph-navigation-button--right";
                break;
            default:
                break;
        }
    }

    protected async Task OnClickHandler(MouseEventArgs ev)
    {
        if (Disabled)
        {
            return;
        }

        await OnClick.InvokeAsync(ev);
    }
}