using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Table;

public partial class PhTable<TTableItem>
{
    /// <summary>
    /// The header row of the table.
    /// </summary>
    [Parameter, EditorRequired] public RenderFragment TableHeader { get; set; } = default!;
    /// <summary>
    /// The rows in the table
    /// </summary>
    [Parameter, EditorRequired] public RenderFragment<TTableItem> TableRow { get; set; } = default!;
    /// <summary>
    /// The list of items that is displayed in the table.
    /// </summary>
    [Parameter, EditorRequired] public Dictionary<string, List<TTableItem>>? Items { get; set; }

    /// <summary>
    /// RowType sets the styling for the datagrid rows.
    /// </summary>
    [Parameter] public RowTypeEnum RowType { get; set; } = RowTypeEnum.ZebraStripes;

    /// <summary>
    /// The event to listen to when an item is selected in the table.
    /// </summary>
    [Parameter] public EventCallback<List<TTableItem>> OnCheckChanged { get; set; }

    private Dictionary<string, List<TTableItem>>? ItemList { get; set; }

    protected string CssClassGrid => new CssBuilder("ph-table")
        .AddClass("ph-table--zebra-stripes", RowType == RowTypeEnum.ZebraStripes)
        .AddClass("ph-table--lines-between-rows", RowType == RowTypeEnum.LinesBetweenRows)
        .Build();

    protected override void OnInitialized()
    {
        if (Items is not null)
        {
            ItemList = Items;
        }
    }

}