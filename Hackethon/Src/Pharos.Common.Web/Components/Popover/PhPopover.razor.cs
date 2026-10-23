using Microsoft.JSInterop;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Popover;

public partial class PhPopover
{
    /// <summary>
    /// The popover panel is aligned to this anchor element's position and size.
    /// Usually contains the button with popovertarget that opens the panel.
    /// Closes on click outside or when Escape is pressed. Use <see cref="Hide"/> to
    /// close the popover programmatically, e.g. when a value is selected or similar.
    /// </summary>
    /// <remarks>
    /// Uses the Popover API for toggling and soft dismissing the popover.
    /// Uses CSS anchor positioning for positioning the popover relative to the edges
    /// of the toggle button/anchor element.
    /// </remarks>
    [Parameter, EditorRequired] public RenderFragment? Anchor { get; set; }

    /// <summary>
    /// The content that is shown when the popover is open. You need a button with a popovertarget attribute pointing
    /// to the <see cref="Id"/> of this component.
    /// </summary>
    [Parameter, EditorRequired] public RenderFragment? Panel { get; set; }

    /// <summary>
    /// Unique ID for the popover panel. Use the same ID in the popovertarget attribute of the button toggling this popover.
    /// If no ID is set, the popover will generate a unique ID.
    /// </summary>
    [Parameter] public string Id { get; set; } = string.Empty;

    [Parameter] public string? CssClasses { get; set; }
    [Parameter] public PopoverStyle Appearance { get; set; } = PopoverStyle.None;
    [Parameter] public PopoverPosition Position { get; set; } = PopoverPosition.Auto;
    [Parameter] public bool FillWidth { get; set; }

    /// <summary>
    /// Hide the popover if the anchor is not in view.
    /// </summary>
    [Parameter] public bool AutoHide { get; set; }
    [Parameter] public EventCallback<bool> OnToggle { get; set; }

    private bool _isOpen;

    /// <summary>
    /// Avoid closing modal dialog that contains a popover when pressing Escape to close the popover.
    /// </summary>
    private bool PreventEscapeKeyPropagation { get; set; }

    private string AnchorName => $"--popover-anchor-{Id}";
    /// <summary>
    /// Setting the anchor name is necessary until the anchor attribute is released (implicit anchor) or until the anchor-scope CSS property is released.
    /// </summary>
    private string AnchorStyle => $"anchor-name: {AnchorName}";
    private string PopoverAnchorStyle => $"position-anchor: {AnchorName}";
    private string? PopoverCss { get; set; }
    private ElementReference? _popoverElement;
    private IJSObjectReference? _popoverScript;

    protected override void OnParametersSet()
    {
        var cssBuilder = new CssBuilder("ph-popover")
            .AddClass("ph-popover--with-background", Appearance != PopoverStyle.None)
            .AddClass("ph-popover--fill-width", FillWidth)
            .AddClass("ph-popover--auto-hide", AutoHide)
            .AddClass("ph-popover--dock-right", Position == PopoverPosition.BottomRight)
            .AddClass("ph-popover--sidebar", Position == PopoverPosition.Sidebar);

        if (!string.IsNullOrWhiteSpace(CssClasses))
        {
            cssBuilder.AddClass(CssClasses);
        }

        PopoverCss = cssBuilder.Build();

        if (string.IsNullOrWhiteSpace(Id))
        {
            Id = Guid.NewGuid().ToString("N");
        }
    }

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _popoverScript = await ImportCommonUtilityJSAsync("popover.js");
        }
    }

    /// <summary>
    /// Hide popover from external component.
    /// </summary>
    public async Task Hide()
    {
        if (_popoverElement is not null && _popoverScript is not null)
        {
            await _popoverScript.InvokeVoidAsync("hidePopover", _popoverElement);
        }
    }

    /// <summary>
    /// Show popover from external component. USE WITH CARE!
    /// </summary>
    /// <remarks>
    /// Don't use this in toggle buttons as it will conflict with soft dismiss of the popover (hiding it and showing it again rapidly).
    /// </remarks>
    public async Task Show()
    {
        // Note: showPopover and togglePopover are supposed to be used with popover="manual", which disables soft dismiss of the popover.
        // Show can be used with popover="auto" but beware of interference with the soft dismiss functionality that will close the popover on
        // click outside or when Escape is pressed. Don't use this method for toggle buttons.

        if (_popoverElement is not null && _popoverScript is not null)
        {
            await _popoverScript.InvokeVoidAsync("showPopover", _popoverElement);
        }
    }

    private async Task ToggleOpen()
    {
        // Toggle the open state when the toggle event is raised, since EventArgs.NewState isn't available.
        _isOpen = !_isOpen;
        PreventEscapeKeyPropagation = _isOpen;
        if (OnToggle.HasDelegate)
        {
            await OnToggle.InvokeAsync(_isOpen);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await _popoverScript.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }
}