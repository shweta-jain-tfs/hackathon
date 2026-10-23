using System.Text.RegularExpressions;

namespace Pharos.Client.Common.App.Services;

/// <summary>
/// Utility class for parsing ProblemDetails information from exception messages.
/// </summary>
public static partial class ExceptionDetailsParser
{
    /// <summary>
    /// Attempts to extract Title and Detail from an exception message.
    /// First tries to parse as JSON ProblemDetails, then falls back to text parsing.
    /// </summary>
    /// <param name="message">The exception message to parse.</param>
    /// <returns>A tuple containing (Title, Detail), or null if parsing fails.</returns>
    internal static ExceptionContent? FromExceptionMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        // If JSON parsing fails, try to extract from structured text
        return TryParseStructuredText(message);
    }

    [GeneratedRegex(@"""title""\s*:\s*""(?<title>[^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex RegExTitle();
    [GeneratedRegex(@"""detail""\s*:\s*""(?<detail>.*?)(?:(?:\\r\\n)|""[,}])", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex RegExDetail();

    private static ExceptionContent? TryParseStructuredText(string message)
    {
        try
        {
            // Regex to find JSON fields "title" and "detail" (handles embedded JSON)
            var titleMatch = RegExTitle().Match(message);
            var detailMatch = RegExDetail().Match(message);

            var title = titleMatch.Success ? titleMatch.Groups["title"].Value : null;
            var detail = detailMatch.Success ? detailMatch.Groups["detail"].Value : null;

            if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(detail))
            {
                return new ExceptionContent(title, detail);
            }
        }
        catch
        {
            // Regex parsing failed
        }

        return null;
    }

}

internal class ExceptionContent
{
    public ExceptionContent(string? title, string? detail)
    {
        Title = title;
        Detail = detail;
    }

    public string? Title { get; set; }
    public string? Detail { get; set; }
}