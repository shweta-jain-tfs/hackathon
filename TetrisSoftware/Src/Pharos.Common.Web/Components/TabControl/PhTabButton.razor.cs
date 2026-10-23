using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.TabControl;

/// <summary>
/// This is the Pharos button called PhButton.
/// </summary>
public partial class PhTabButton
{
    /// <summary>
    /// Child Content to be able to add texts.
    /// Can also be used of other htlm elements to display inside the button.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Click event for sending back an event that the button has been clicked on.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    /// disables the button click event and sets it to the disabled styling.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public TabControlStyle TabControlStyle { get; set; }

    protected string CssClasses => new CssBuilder("ph-tab-button")
        .AddClass($"ph-tab-button--style-primary", TabControlStyle == TabControlStyle.Primary)
        .AddClass($"ph-tab-button--style-secondary", TabControlStyle == TabControlStyle.Secondary)
        .AddClass($"ph-tab-button--selected", Selected)
        .Build();


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