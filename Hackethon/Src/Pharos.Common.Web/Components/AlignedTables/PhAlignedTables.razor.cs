using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.AlignedTables;

/// <summary>
/// Renders a container for aligning columns across multiple tables.
/// Only accepts <see cref="PhAlignedTableItem"/> as child content.
/// </summary>
public partial class PhAlignedTables
{
    /// <summary>
    /// Number of columns to align across all tables.
    /// </summary>
    [Parameter] public ColumnCount Columns { get; set; } = ColumnCount.Eight;

    /// <summary>
    /// The table items to align. Only <see cref="PhAlignedTableItem"/> is supported.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string CssClasses => new CssBuilder("ph-aligned-tables")
        .AddClass("ph-aligned-tables--cols-4", Columns == ColumnCount.Four)
        .AddClass("ph-aligned-tables--cols-5", Columns == ColumnCount.Five)
        .AddClass("ph-aligned-tables--cols-6", Columns == ColumnCount.Six)
        .AddClass("ph-aligned-tables--cols-7", Columns == ColumnCount.Seven)
        .Build();
}