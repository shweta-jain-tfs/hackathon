using Microsoft.JSInterop;

namespace Pharos.Common.Web.Components.DataGrid;

/// <summary>
/// Helper methods for PhDataGrid scrolling functionality
/// </summary>
public static class PhDataGridScrollHelper
{
    /// <summary>
    /// Scrolls the grid to a specific row index and centers it.
    /// Waits for rows to be available, then calculates scroll position based on row index.
    /// </summary>
    /// <param name="js">JSRuntime for JavaScript interop</param>
    /// <param name="targetRowIndex">Zero-based index of the row to scroll to and center</param>
    /// <param name="maxWaitMs">Maximum time to wait for rows to appear (default 5000ms)</param>
    /// <param name="rowHeight">Row height in pixels (default 56, auto-detected if rows exist)</param>
    /// <param name="gridContainerSelector">Optional CSS selector for the grid container</param>
    /// <param name="gridContainerRef">Optional ElementReference for the grid container</param>
    /// <returns>True if scroll succeeded, false if rows never appeared</returns>
    public static async Task<bool> ScrollToRowAsync(
        IJSRuntime js,
        int targetRowIndex,
        int maxWaitMs = 5000,
        int rowHeight = 56,
        string gridContainerSelector = ".ph-datagrid__container",
        ElementReference? gridContainerRef = null)
    {
        if (targetRowIndex < 0)
        {
            return false;
        }

        try
        {
            var module = await js.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Pharos.Common.Web/js/PhDataGrid.js");

            try
            {
                object container = gridContainerRef.HasValue ? gridContainerRef.Value : gridContainerSelector;
                return await module.InvokeAsync<bool>("scrollToRowIndex",
                    container,
                    targetRowIndex,
                    new
                    {
                        maxWaitMs,
                        rowHeight
                    });
            }
            finally
            {
                await module.DisposeAsync();
            }
        }
        catch
        {
            return false;
        }
    }
}
