namespace Pharos.Common.Web.Components.DataGrid.Filters.Quick;

using Pharos.Common.Web.Components.DataGrid.Filters.Constraints;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;

public partial class PhQuickNumericColumnFilter<TGridItem, TProp>
{
    public double? LessThan { get; set; }
    public double? GreaterThan { get; set; }

    private int? _syncedResetRevision;

    private INumericFilterConstraints? NumericConstraints =>
        ColumnFilter.Column.FilterConstraints as INumericFilterConstraints;

    private bool AllowNegativeNumbers => NumericConstraints?.AllowNegativeNumbers ?? false;

    protected override void OnParametersSet()
    {
        if (ColumnFilter.ResetRevision != _syncedResetRevision)
        {
            _syncedResetRevision = ColumnFilter.ResetRevision;
            LessThan = GetSelectedValueOrNull(ColumnFilter.LessThanValue);
            GreaterThan = GetSelectedValueOrNull(ColumnFilter.GreaterThanValue);
        }
    }

    protected override void ResetLocalState()
    {
        LessThan = GreaterThan = null;
    }

    private async Task OnSearch()
    {
        if (GreaterThan.HasValue)
        {
            ColumnFilter.GreaterThanValue.IsSelected = true;
            ColumnFilter.GreaterThanValue.Value = GreaterThan.Value;
        }
        else
        {
            ColumnFilter.GreaterThanValue.IsSelected = false;
        }

        if (LessThan.HasValue)
        {
            ColumnFilter.LessThanValue.IsSelected = true;
            ColumnFilter.LessThanValue.Value = LessThan.Value;
        }
        else
        {
            ColumnFilter.LessThanValue.IsSelected = false;
        }

        await OnValueChanged.InvokeAsync();
    }

    private static double? GetSelectedValueOrNull(NumericFilterValues filterValue)
    {
        if (!filterValue.IsSelected)
        {
            return null;
        }

        return filterValue.Value;
    }
}
