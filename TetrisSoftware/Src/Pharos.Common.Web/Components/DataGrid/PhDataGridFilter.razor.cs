using Pharos.Common.Web.Components.DataGrid.Filters.Quick;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid;

public partial class PhDataGridFilter<TGridItem>
{
    [Parameter] public List<IQuickFilterSource> Filters { get; set; } = default!;
    [Parameter] public EventCallback<IEnumerable<IColumnFilter>> OnFilterChanged { get; set; }

    private List<IQuickFilterSource> QuickFilterColumns { get; set; } = new();

    protected override void OnParametersSet()
    {
        QuickFilterColumns = Filters
            .Where(c => c is { SupportsFiltering: true, Filter: not null, QuickFilter: true, QuickFilterComponentType: not null })
            .ToList();
    }

    private IEnumerable<IColumnFilter> Filtered => Filters
        .Where(source => source is { SupportsFiltering: true, Filter: not null })
        .Select(source => source.Filter!);

    private async Task ClearFilter()
    {
        foreach (var filter in Filtered)
        {
            filter.Clear();
        }

        await FilterChanged();
    }


    private async Task ResetToDefault()
    {
        foreach (var filter in Filtered)
        {
            filter.ResetToDefault();
        }

        await FilterChanged();
    }

    private async Task FilterChanged()
    {

        await OnFilterChanged.InvokeAsync(Filtered);
    }

    private static Dictionary<string, object> GenerateQuickFilterParameters(IColumnFilter columnFilter, EventCallback onValueChanged)
    {
        return new Dictionary<string, object>
        {
            { nameof(PhQuickStringColumnFilter<,>.ColumnFilter), columnFilter },
            { nameof(PhQuickStringColumnFilter<,>.OnValueChanged), onValueChanged }
        };
    }
}
