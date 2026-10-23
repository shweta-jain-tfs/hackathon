using Microsoft.AspNetCore.Components.Rendering;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Columns;
/// <summary>
/// Represents the base class for a column in the data grid.
/// </summary>
/// <typeparam name="TGridItem">The type of the grid item.</typeparam>
public abstract class ColumnBase<TGridItem> : ComponentBase, IFilterableColumn
{
    /// <summary>
    /// Gets or sets the item associated with the column.
    /// </summary>
    [CascadingParameter(Name = "Item"), EditorRequired]
    public TGridItem Item { get; set; } = default!;

    /// <summary>
    /// Gets or sets the title of the column.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the title used by the filter UI for this column.
    /// Falls back to <see cref="Title" /> when not provided.
    /// </summary>
    [Parameter]
    public string? FilterTitle { get; set; }

    public string? EffectiveFilterTitle => FilterTitle ?? Title;

    /// <summary>
    /// Gets or sets a value indicating whether the column supports quick filtering.
    /// </summary>
    [Parameter]
    public bool QuickFilter { get; set; }

    /// <summary>
    /// Gets or sets the default value for a quick filter.
    /// </summary>
    [Parameter]
    public object? QuickFilterDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is sortable.
    /// </summary>
    [Parameter]
    public bool? Sortable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column is visible.
    /// </summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the alignment of the column content.
    /// </summary>
    [Parameter]
    public ColumnAlignEnum Align { get; set; } = ColumnAlignEnum.Auto;

    /// <summary>
    /// Optionally allow the column header to break into two lines or fill remaining space.
    /// </summary>
    [Parameter] public ColumnSizeEnum Size { get; set; } = ColumnSizeEnum.Auto;

    /// <summary>
    /// Gets or sets the format string for the column content.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>
    /// Indicates whether the user has permission to see this column. Default is true.
    /// </summary>
    [Parameter]
    public bool HasPermission { get; set; } = true;

    /// <summary>
    /// Skip repeated values in this column as part of a left-to-right grouping.
    /// Grouped columns must be adjacent and start at the first visible column.
    /// A value is shown when this column or a grouped column to the left changes.
    /// </summary>
    /// <remarks>
    /// Grouping is only applied when the grid row style is <see cref="Pharos.Common.Web.Components.DataGrid.RowTypeEnum.LinesBetweenRows" />.
    /// </remarks>
    [Parameter]
    public bool Grouped { get; set; }

    public bool SupportsFiltering => HasPermission && QuickFilter;

    /// <summary>
    /// Gets or sets the property name associated with the column.
    /// </summary>
    public virtual string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the column is being dragged over.
    /// </summary>
    public bool IsDragOver { get; set; }

    /// <summary>
    /// Gets or sets the display order of the column.
    /// Columns are sorted by this field in ascending order before rendering.
    /// The column with the smallest order value becomes the leftmost.
    /// </summary>
    public int ColumnOrder { get; set; }

    /// <summary>
    /// Gets or sets the filter associated with the column.
    /// </summary>
    public IColumnFilter? Filter { get; set; }

    /// <summary>
    /// Gets or sets the type of the quick filter component for the column.
    /// </summary>
    public Type? QuickFilterComponentType { get; set; }

    /// <summary>
    /// Gets or sets the initial sort direction for the column.
    /// </summary>
    public SortDirection InitialSortDirection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this column should be sorted by default.
    /// </summary>
    public bool IsDefaultSortColumn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column content should be aligned to the right.
    /// </summary>
    public bool AlignRight { get; set; }

    /// <summary>
    /// Gets or sets the type of the property associated with the column.
    /// </summary>
    public virtual Type? PropertyType { get; set; }

    public bool HasCustomContent { get; set; }

    /// <summary>
    /// Forces this column to use string filter components with exact-match semantics.
    /// Useful when displaying non-string values (for example integers) that should only match full input.
    /// </summary>
    [Parameter]
    public bool ForceExactMatchFilter { get; set; }

    internal abstract object? GetValue(TGridItem item);

    /// <summary>
    /// Provides rendering logic for the column's cells.
    /// Derived components can override this method to customize cell rendering.
    /// </summary>
    /// <param name="builder">The current <see cref="RenderTreeBuilder" />.</param>
    /// <param name="item">The data for the row being rendered.</param>
    internal virtual void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
    }
}
