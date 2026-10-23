namespace Pharos.Common.Web.Components.AlignedTables;

/// <summary>
/// Represents a single table to be aligned within <see cref="PhAlignedTables"/>.
/// 
/// Usage notes:
/// - Set <c>Title</c> to add a section title above the table.
/// - Place a <c>&lt;table&gt;</c> element inside this item.
/// - To enable cell wrapping, add the <c>ph-aligned-tables__cell-wrap</c> class to table cells.
/// - If a table has fewer columns than others, use <c>colspan</c> on cells to keep columns aligned across all tables.
/// </summary>
public partial class PhAlignedTableItem
{
    /// <summary>
    /// The table markup to render inside this item.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Optional title for the table section.
    /// </summary>
    [Parameter] public string? Title { get; set; }
}