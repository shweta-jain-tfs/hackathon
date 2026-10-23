using System.Net;
using System.Text.RegularExpressions;

namespace Pharos.Common.Web.Utilities;

public static class TextHighlighter
{
    public static MarkupString Highlight(string? text, string? searchText, string highlightClass = "font-bold")
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(searchText))
        {
            return new(WebUtility.HtmlEncode(text ?? ""));
        }

        var pattern = Regex.Escape(searchText);

        var parts = Regex.Split(
            text,
            $"({pattern})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );

        var result = string.Concat(parts.Select(part =>
            string.Equals(part, searchText, StringComparison.OrdinalIgnoreCase)
                ? $"<span class=\"{highlightClass}\">{WebUtility.HtmlEncode(part)}</span>"
                : WebUtility.HtmlEncode(part)
        ));

        return new(result);
    }

}
