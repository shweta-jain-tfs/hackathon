namespace Pharos.Common.Web.Components.DataGrid;

public enum ColumnSizeEnum
{
    /// <summary>
    /// Default behavior, the column size is determined automatically.
    /// </summary>
    Auto,

    /// <summary>
    /// The column title can break to two lines.
    /// </summary>
    WrapTitle,

    /// <summary>
    /// The column fills the remaining space.
    /// </summary>
    Fill,
}
