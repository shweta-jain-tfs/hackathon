using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Icon-only button. Can have a background or not and supports different shapes.
/// The icon size determines the size of the button.
/// </summary>
public partial class PhIconButton
{
    /// <summary>
    /// Disables the button and applies disabled styling.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Sets the shape of the button.
    /// </summary>
    [Parameter]
    public IconButtonType Type { get; set; } = IconButtonType.Standard;

    /// <summary>
    /// Set size of icon.
    /// </summary>
    [Parameter]
    public IconSize IconSize { get; set; } = Icons.IconSize.Md;

    /// <summary>
    /// Set what icon to show.
    /// </summary>
    [Parameter, EditorRequired]
    public IconType IconType { get; set; } = IconType.Warning;

    /// <summary>
    /// Click event for sending back an event that the button has been clicked on.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Sets the button to an selected state, with different style.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; } = false;

    /// <summary>
    /// ID of the [popover] element to toggle with this button.
    /// </summary>
    [Parameter]
    public string? PopoverTarget { get; set; }

    protected string Classname => new CssBuilder("ph-iconbutton")
        .AddClass("ph-iconbutton--secondary-button", Type is IconButtonType.SecondaryButton)
        .AddClass("ph-iconbutton--standard", Type is IconButtonType.Standard)
        .AddClass("ph-iconbutton--menu", Type is IconButtonType.Menu)
        .AddClass("ph-iconbutton--blank", Type is IconButtonType.Blank)
        .AddClass("ph-iconbutton--selected", Selected)
        .Build();

    protected async Task OnClickHandler(MouseEventArgs ev)
    {
        if (Disabled)
        {
            return;
        }

        await OnClick.InvokeAsync(ev);
    }
}