using System.Text.RegularExpressions;

namespace Pharos.Client.Common.App.HelpContent;

internal static partial class HelpContentUrlRewriter
{
    public static string RewriteUrls(string content)
    {
        return ContentUrlRegex().Replace(content, match =>
        {
            var attribute = match.Groups["attribute"].Value;
            var quotedValue = match.Groups["quotedValue"].Value;
            var value = quotedValue.Trim(['"', '\'']);

            var isImage = attribute.Equals("src", StringComparison.OrdinalIgnoreCase);
            var rewritten = isImage
                ? RewriteImageSrc(value)
                : RewriteHelpLink(value);

            if (!isImage && string.IsNullOrEmpty(rewritten))
            {
                // Remove unsupported href attributes.
                return string.Empty;
            }

            return match.Value.Replace(
                quotedValue,
                $"\"{rewritten}\"",
                StringComparison.Ordinal);
        });
    }

    public static string RewriteHelpLink(string href)
    {
        var helpRoute = $"/{PageRoute.Help.Trim('/')}/#";
        if (href.StartsWith(helpRoute))
        {
            // Already in the correct format
            return href;
        }

        if (href.StartsWith('#'))
        {
            // Expected from exported help content
            return $"{helpRoute}{href[1..]}";
        }

        const string helpLinkPrefix = "{{HelpLink-";
        if (href.StartsWith(helpLinkPrefix) && href.EndsWith("}}"))
        {
            // According to spec for the export
            return $"{helpRoute}{href[helpLinkPrefix.Length..^2]}";
        }

        // Add more supported link formats here

        // Unsupported link, clear
        return string.Empty;
    }

    public static string RewriteImageSrc(string src)
    {
        const string basePath = "/_content/Pharos.Client.App.Help/manuals";

        if (string.IsNullOrWhiteSpace(src))
        {
            return src;
        }

        if (src.StartsWith(basePath))
        {
            // already looks correct
            return src;
        }

        return $"{basePath}/{src.TrimStart('/')}";
    }

    [GeneratedRegex("""\s+(?<attribute>src|href)=(?<quotedValue>["'][^"']*["'])""", RegexOptions.IgnoreCase)]
    private static partial Regex ContentUrlRegex();
}
