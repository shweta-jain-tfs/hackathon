namespace Pharos.Common.Web.Models.DataGrid;

/// <summary>
/// Represents a source that can render a quick filter component.
/// </summary>
public interface IQuickFilterSource
{
    /// <summary>
    /// Filter definition associated with the quick filter.
    /// </summary>
    IColumnFilter? Filter { get; }

    /// <summary>
    /// Component type used to render the quick filter UI.
    /// </summary>
    Type? QuickFilterComponentType { get; }

    /// <summary>
    /// Indicates whether the source is shown as a quick filter.
    /// </summary>
    bool QuickFilter { get; }

    /// <summary>
    /// Indicates whether the source supports any type of filtering (regular or quick filter).
    /// </summary>
    bool SupportsFiltering { get; }

    /// <summary>
    /// Position of a custom quick filter relative to the column quick filters.
    /// Only used for sources passed via <c>PhDataGrid.CustomQuickFilters</c>; ignored for columns.
    /// </summary>
    CustomQuickFilterPosition Position => CustomQuickFilterPosition.Start;
}
