namespace Pharos.Common.Web.Models.DataGrid;

/// <summary>
/// Where a custom quick filter is placed relative to the column quick filters.
/// </summary>
public enum CustomQuickFilterPosition
{
    /// <summary>Placed before the column quick filters (default).</summary>
    Start,

    /// <summary>Placed after the column quick filters.</summary>
    End
}