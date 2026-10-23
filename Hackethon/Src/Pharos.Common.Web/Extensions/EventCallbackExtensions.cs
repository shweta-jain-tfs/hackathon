using System.Reflection;

namespace Pharos.Common.Web.Extensions;

public static class EventCallbackExtensions
{
    public static string? TryGetHandlerName(this EventCallback callback)
    {
        return callback.HasDelegate ? TryGetName(callback) : null;
    }

    public static string? TryGetHandlerName<T>(this EventCallback<T> callback)
    {
        return callback.HasDelegate ? TryGetName(callback) : null;
    }

    private static string? TryGetName(object boxedCallback)
    {
        // EventCallback and EventCallback<T> are different struct types.
        // Get the private delegate field from the runtime type so it works in both WASM and Server.
        var type = boxedCallback.GetType();
        var field =
            type.GetField("_delegate", BindingFlags.Instance | BindingFlags.NonPublic) ??
            type.GetField("Delegate", BindingFlags.Instance | BindingFlags.NonPublic);

        var methodName = (field?.GetValue(boxedCallback) as Delegate)?.Method?.Name;
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return null;
        }

        // Ignore generated wrapper methods (lambdas / Razor-generated / dynamic methods)
        if (methodName.StartsWith('<') ||
            methodName.StartsWith("lambda_method", StringComparison.Ordinal))
        {
            return null;
        }

        return methodName;
    }
}