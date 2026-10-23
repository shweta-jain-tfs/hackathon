using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// The standard button component used throughout the Pharos UI.
/// Supports primary, secondary, and text visual styles, in a standard size by default.
/// </summary>
public partial class PhButton
{
    /// <summary>
    /// Content rendered inside the button (text or other elements).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Invoked when the button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Controls the visual style of the button (Primary, Secondary, or Text).
    /// </summary>
    [Parameter]
    public ButtonType Type { get; set; } = Enums.ButtonType.Secondary;

    /// <summary>
    /// Disables the button and applies disabled styling.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Sets the button to an selected state, with different style.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; } = false;

    /// <summary>
    /// The HTML button type attribute.
    /// Defaults to "button" to avoid accidental form submission; set to "submit" for form buttons.
    /// </summary>
    [Parameter]
    public string? ButtonType { get; set; } = "button";

    /// <summary>
    /// Controls the button size. Default is the standard size used in most of the application.
    /// </summary>
    [Parameter]
    public ButtonSize Size { get; set; } = ButtonSize.Standard;

    /// <summary>
    /// Removes the default max-width constraint, allowing the button to expand.
    /// </summary>
    [Parameter]
    public bool IgnoreMaxWidth { get; set; } = false;

    /// <summary>
    /// Icon displayed before the text.
    /// </summary>
    [Parameter]
    public IconType? StartIconType { get; set; }

    /// <summary>
    /// Icon displayed after the text.
    /// </summary>
    [Parameter]
    public IconType? EndIconType { get; set; }

    /// <summary>
    /// Prevents the click event from bubbling. Typically paired with <see cref="PreventDefault"/>.
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; } = true;

    /// <summary>
    /// Prevents the default click behavior. Automatically disabled when <see cref="PopoverTarget"/> is set.
    /// </summary>
    [Parameter]
    public bool PreventDefault { get; set; } = true;

    /// <summary>
    /// The ID of a popover element to toggle when clicking this button.
    /// </summary>
    [Parameter]
    public string? PopoverTarget { get; set; }

    /// <summary>
    /// Whether the user has permission to see this button. Defaults to true.
    /// </summary>
    [Parameter]
    public bool HasPermission { get; set; } = true;

    /// <summary>
    /// Optional ID attribute for the button element, useful for testing or accessibility purposes.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    protected string? AutomatedTestId { get; set; }

    protected string Classname => new CssBuilder("ph-button")
        .AddClass($"ph-button--primary", Type == Enums.ButtonType.Primary)
        .AddClass($"ph-button--secondary", Type == Enums.ButtonType.Secondary)
        .AddClass($"ph-button--text", Type == Enums.ButtonType.Text)
        .AddClass("ph-button--selected", Selected)
        .AddClass("ph-button--snug", Size == ButtonSize.Snug)
        .AddClass("ph-button--no-max-width", IgnoreMaxWidth)
        .Build();

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(PopoverTarget))
        {
            // can't prevent default in conjunction with popovertarget
            PreventDefault = false;
        }

        var name = !string.IsNullOrWhiteSpace(Id) ? Id : OnClick.TryGetHandlerName();
        AutomatedTestId = name is null ? null : this.CreateAutomatedTestId(name);
    }

    protected async Task OnClickHandler(MouseEventArgs ev)
    {
        if (Disabled)
        {
            return;
        }

        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(ev);
        }
    }
}