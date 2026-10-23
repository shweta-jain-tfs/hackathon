namespace Pharos.Common.Web.Components.DataGrid;

[Flags]
public enum ItemStyleFlags
{
    /// <summary>
    /// No item-specific styling.
    /// </summary>
    None = 0,

    /// <summary>
    /// Disables the selection checkbox and dims the row.
    /// </summary>
    Disabled = 1 << 0,

    /// <summary>
    /// Displays the selection checkbox as checked without changing the selection state.
    /// </summary>
    Checked = 1 << 1,

    /// <summary>
    /// Dims the row without disabling its selection checkbox.
    /// </summary>
    Dimmed = 1 << 2,

    /// <summary>
    /// Disables the selection checkbox without dimming the row.
    /// </summary>
    CheckboxDisabled = 1 << 3,

    /// <summary>
    /// Hides the selection checkbox without dimming the row.
    /// </summary>
    CheckboxHidden = 1 << 4
}
