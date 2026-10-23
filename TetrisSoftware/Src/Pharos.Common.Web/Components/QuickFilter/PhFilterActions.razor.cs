using Pharos.Common.Web.Components.Popover;
using Pharos.Common.Web.Contexts;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.QuickFilter;

public partial class PhFilterActions
{
    [CascadingParameter(Name = "UiContext")] public UiContext? UiContext { get; set; }
    [Parameter] public EventCallback OnClearAll { get; set; }
    [Parameter] public EventCallback OnResetToDefault { get; set; }

    private PhPopover? _popover;
    private bool _isOpen;
    private string ButtonCssClasses => new CssBuilder("ph-filter-button ph-filter-button--icon")
        .AddClass("ph-filter-button--selected", _isOpen)
        .Build();

    public DataGridStrings Strings { get; set; } = DataGridStrings.Default;

    protected override void OnParametersSet()
    {
        Strings = UiContext?.ComponentStrings.DataGrid ?? DataGridStrings.Default;
    }

    private void SetOpenState(bool value)
    {
        _isOpen = value;
    }

    private async Task ClosePanel(MouseEventArgs args)
    {
        if (_popover is not null)
        {
            await _popover.Hide();
        }
    }
}