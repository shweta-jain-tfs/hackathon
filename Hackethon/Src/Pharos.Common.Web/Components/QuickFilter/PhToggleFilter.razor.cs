using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.QuickFilter;

public partial class PhToggleFilter
{
    [Parameter] public bool Value { get; set; }
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }
    [Parameter] public string? Label { get; set; }

    private string ButtonCssClasses => new CssBuilder("ph-filter-button ph-filter-button--toggle")
        .AddClass("ph-filter-button--selected", Value)
        .Build();

    private Task Toggle()
    {
        return ValueChanged.InvokeAsync(!Value);
    }
}