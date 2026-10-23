using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.TabControl;

public partial class PhTabPage : ComponentBase
{
    [CascadingParameter] private PhTabControl? Parent { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Disable the trailing edge (gradient) for scrolling tab content.
    /// Set this to true if there is a <see cref="DataGrid.PhDataGrid{TGridItem}"/> with a trailing edge inside the tab.
    /// </summary>
    [Parameter] public bool HideTrailingEdge { get; set; }

    [Parameter] public bool HasValidationError { get; set; }

    [Parameter] public TabControlSymbolType SymbolType { get; set; } = TabControlSymbolType.None;

    [Parameter] public int? TabId { get; set; }

    [Parameter] public required string Text { get; set; }

    private string CssClasses { get; set; } = "";

    /// <summary>
    /// The effective ID used internally by the tab control.
    /// Uses TabId if set, otherwise assigned by parent TabControl.
    /// </summary>
    internal int EffectiveTabId { get; set; }

    protected override void OnInitialized()
    {
        if (Parent == null)
        {
            throw new ArgumentNullException(nameof(Parent), "TabPage must exist within a TabControl");
        }

        Parent.AddPage(this);
    }

    protected override void OnParametersSet()
    {
        CssClasses = new CssBuilder("ph-tab-control__content")
            .AddClass("hidden", Parent?.SelectedPage != this)
            .Build();
    }

    public StatusIconType? StatusIcon =>
        SymbolType switch
        {
            TabControlSymbolType.Alarm => StatusIconType.Alarm,
            TabControlSymbolType.Alert => StatusIconType.Alert,
            TabControlSymbolType.Warning => StatusIconType.Warning,
            _ => HasValidationError ? StatusIconType.Alert : null
        };
}