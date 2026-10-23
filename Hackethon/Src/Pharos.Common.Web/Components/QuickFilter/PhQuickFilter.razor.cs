using Pharos.Common.Web.Components.Popover;
using Pharos.Common.Web.Contexts;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.QuickFilter;

public partial class PhQuickFilter
{
    [CascadingParameter(Name = "UiContext")] public UiContext? UiContext { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public int ActiveFilterCount { get; set; }
    [Parameter] public QuickFilterPanelSize PanelSize { get; set; } = QuickFilterPanelSize.Default;

    [Parameter] public EventCallback OnClearFilter { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private PhPopover? _popover;
    private bool _isOpen;
    private string ButtonCssClasses => new CssBuilder("ph-filter-button")
        .AddClass("ph-filter-button--selected", _isOpen || ActiveFilterCount > 0)
        .Build();

    private string PanelCssClasses => new CssBuilder("ph-filter-panel")
        .AddClass("ph-filter-panel--wide", PanelSize == QuickFilterPanelSize.Spacious)
        .AddClass("ph-filter-panel--loose", PanelSize == QuickFilterPanelSize.Spacious)
        .Build();

    private string? ClearButtonLabel => UiContext?.ComponentStrings.DataGrid.ClearFilter ?? DataGridStrings.Default.ClearFilter;

    private async Task SetOpenState(bool value)
    {
        var wasOpen = _isOpen;
        _isOpen = value;

        if (wasOpen && !_isOpen)
        {
            await OnClose.InvokeAsync();
        }
    }

    private async Task ClearFilter()
    {
        await OnClearFilter.InvokeAsync();
    }
}