using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.DockedButtonRow;

/// <summary>
/// Places a row of buttons at the bottom of a <see cref="DataGrid.PhDataGrid{TGridItem}"/> or other content.
/// Keeps the button row positioned directly below the content unless <see cref="DockToBottom"/> is set,
/// in which case the buttons are placed as far down as possible with space between the content and the buttons.
/// </summary>
public partial class PhDockedButtonRow
{
    /// <summary>
    /// The content to be displayed above the button row.
    /// </summary>
    [Parameter][EditorRequired] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The buttons to be displayed in the button row.
    /// </summary>
    [Parameter][EditorRequired] public RenderFragment? Buttons { get; set; }

    /// <summary>
    /// Optional content to be displayed on the left side of the button row.
    /// </summary>
    [Parameter] public RenderFragment? LeftContent { get; set; }

    /// <summary>
    /// The text to display alongside the selected count. If provided, will be combined with the selected count
    /// to show selection information (e.g., "Selected: 3" when SelectedText is "Selected:" and 3 items are selected).
    /// </summary>
    [Parameter] public string? SelectedText { get; set; }

    /// <summary>
    /// Indicates whether the button row is dispalyed.
    /// default is true
    /// </summary>
    [Parameter] public bool EnableButtons { get; set; } = true;

    /// <summary>
    /// Specifies whether the buttons in the row should have equal width.
    /// </summary>
    [Parameter]
    public bool Justified { get; set; } = true;

    /// <summary>
    /// Determines whether the button row should be docked to the bottom of the container,
    /// even if <see cref="ChildContent"/> does not fill the height of the page or container.
    /// </summary>
    [Parameter]
    public bool DockToBottom { get; set; }


    // Delegate set by ButtonRowWithCount
    public Action<int>? SelectedCountChanged { get; set; }

    /// <summary>
    /// Gets the CSS class names for the button row based on the component's state.
    /// </summary>
    private string ClassNames => new CssBuilder("ph-docked-row")
        .AddClass("ph-docked-row--fill", DockToBottom)
        .AddClass("ph-docked-row--no-buttons", !EnableButtons)
        .Build();

    // Called by grid or selection logic
    internal void RaiseSelectedCount(int count)
    {
        // Do NOT call StateHasChanged here.
        SelectedCountChanged?.Invoke(count);
    }
}