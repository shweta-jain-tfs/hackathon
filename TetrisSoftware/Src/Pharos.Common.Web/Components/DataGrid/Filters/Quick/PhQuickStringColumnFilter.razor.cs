namespace Pharos.Common.Web.Components.DataGrid.Filters.Quick;

using Pharos.Common.Web.Components.DataGrid.Filters.Constraints;

public partial class PhQuickStringColumnFilter<TGridItem, TProp>
{
    private string? _columnValue;

    private string? ColumnValue
    {
        get => _columnValue;
        set
        {
            if (_columnValue == value)
            {
                return;
            }

            _columnValue = value;

            if (ColumnFilter.Value != _columnValue)
            {
                ColumnFilter.Value = _columnValue ?? string.Empty;
                OnValueChanged.InvokeAsync();
            }
        }
    }
    private int MaxLength =>
        (ColumnFilter.Column.FilterConstraints as StringFilterConstraints)?.MaxLength ?? 50;

    protected override void OnParametersSet()
    {
        // Sync local value with filter value after initialization or state change
        if (_columnValue != ColumnFilter.Value)
        {
            _columnValue = ColumnFilter.Value;
        }
    }
}
