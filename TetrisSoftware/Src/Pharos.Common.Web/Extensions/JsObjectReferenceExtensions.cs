using Microsoft.JSInterop;

namespace Pharos.Common.Web.Extensions;

public static class JsObjectReferenceExtensions
{
    /// <summary>
    /// Disposes JS modules safely, performing a null check and suppressing exceptions from circuit disconnects (Blazor Server).
    /// Use when disposing JS modules to avoid errors in Stories or server-side scenarios.
    /// </summary>
    public static async ValueTask SafeDisposeAsync(this IJSObjectReference? jsObject)
    {
        if (jsObject is null)
        {
            return;
        }

        try
        {
            await jsObject.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone – ignore (e.g., story switched)
        }
        catch (ObjectDisposedException)
        {
            // Already disposed – ignore
        }
    }
}