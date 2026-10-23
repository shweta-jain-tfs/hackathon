namespace Pharos.Common.Web.Components.DataGrid;

/// <summary>
/// Row height in the data grid. Also affects padding and font size.
/// </summary>
public enum RowHeightEnum
{
    Relaxed,
    Compact,
    /// <summary>
    /// Note: Miminal height rows should not contain clickable content, since the touch area will be too small.
    /// </summary>
    Minimal
}
