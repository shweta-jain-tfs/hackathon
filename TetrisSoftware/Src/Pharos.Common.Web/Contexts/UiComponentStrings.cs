namespace Pharos.Common.Web.Contexts;

/// <summary>
/// Shared strings for complex, application-wide UI components (e.g. DataGrid, NavigationLock).
/// Avoids passing the same static string parameters throughout the component tree.
///
/// Only add strings that:
/// - Are stable and used consistently across the entire application.
/// - Would otherwise be repeatedly passed as identical parameters.
///
/// DO NOT:
/// - Add strings for rarely used or single-use components.
/// - Use this for basic controls (buttons, labels, inputs, etc.).
///   Those must receive text explicitly via parameters.
/// </summary>
public record UiComponentStrings
{
    public required DataGridStrings DataGrid { get; set; }
    public required NavigationLockStrings NavigationLock { get; set; }

    public static UiComponentStrings Default => new()
    {
        DataGrid = DataGridStrings.Default,
        NavigationLock = NavigationLockStrings.Default
    };
}
