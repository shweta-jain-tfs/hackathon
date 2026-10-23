using Pharos.Common.Web.Components.DataGrid.Filters.Models;

namespace Pharos.Common.Web.Components.DataGrid.State;

internal class DataGridState
{
    public List<ColumnFilterState> Filters { get; set; } = new();
    public string? SortColumn { get; set; }
    public bool SortAscending { get; set; }

    /// <summary>
    /// Save the vertical scroll position of the grid.
    /// The grid could be scrolled without changing the start index
    /// due to over scanning.
    /// </summary>
    public double ScrollY { get; set; }
}
