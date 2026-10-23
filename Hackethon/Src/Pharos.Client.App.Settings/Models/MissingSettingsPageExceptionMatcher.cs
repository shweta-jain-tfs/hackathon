namespace Pharos.Client.App.Settings.Models;

internal static class MissingSettingsPageExceptionMatcher
{
    public static bool IsMatch(Exception exception, string? key = null)
    {
        return exception switch
        {
            ApiException apiException => IsMatch(apiException.Response, key) ||
                IsMatch(apiException.Message, key),
            ArgumentException argumentException => IsMatch(argumentException.Message, key),
            _ => false
        };
    }

    private static bool IsMatch(string? message, string? key)
    {
        return !string.IsNullOrWhiteSpace(message) &&
            message.Contains("Page with key", StringComparison.Ordinal) &&
            (key is null || message.Contains(key, StringComparison.Ordinal)) &&
            message.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }
}
