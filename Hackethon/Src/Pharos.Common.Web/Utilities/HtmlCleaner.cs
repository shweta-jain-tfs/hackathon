using System.Net;
using System.Text.RegularExpressions;

namespace Pharos.Common.Web.Utilities;

/// <summary>
/// Simple HTML cleaner that keeps only allowed tags and attributes.
/// Matches the styling expectations of ph-richtext.scss component.
/// </summary>
/// <remarks>
/// Note that images should ideally have height and width attributes set to avoid layout shifts.
/// </remarks>
public static partial class HtmlCleaner
{
    /// <summary>
    /// Cleans HTML content and returns it as a MarkupString for use in Blazor components.
    /// Automatically adds lazy loading to images for better performance.
    /// Disallowed tags are stripped, leaving their content as plain text.
    /// </summary>
    /// <param name="html">The HTML snippet to clean</param>
    /// <returns>Cleaned HTML content as MarkupString with lazy loading</returns>
    public static MarkupString ToCleanRichText(string? html)
    {
        return new MarkupString(CleanAndOptimizeHtml(html));
    }

    /// <summary>
    /// Cleans HTML content and adds optimizations (lazy loading) but returns as string.
    /// Useful when you need to do additional processing before converting to MarkupString.
    /// </summary>
    /// <param name="html">The HTML snippet to clean</param>
    /// <param name="preserveRejectedTags">When true, rejected tags are HTML-encoded instead of removed (useful for debugging)</param>
    /// <returns>Cleaned and optimized HTML content as string</returns>
    public static string CleanAndOptimizeHtml(string? html, bool preserveRejectedTags = false)
    {
        var cleanedHtml = CleanHtml(html, preserveRejectedTags);
        return AddLazyLoadingToImages(cleanedHtml);
    }

    /// <summary>
    /// Cleans HTML content by keeping only allowed tags and attributes.
    /// Disallowed tags are stripped, leaving their content as plain text.
    /// </summary>
    /// <param name="html">The HTML snippet to clean</param>
    /// <param name="preserveRejectedTags">When true, rejected tags are HTML-encoded instead of removed (useful for debugging)</param>
    /// <returns>Cleaned HTML content</returns>
    public static string CleanHtml(string? html, bool preserveRejectedTags = false)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }

        // Allowed tags with their allowed attributes - matches ph-richtext.scss expectations
        var allowedTags = new HashSet<string>
        {
            "h1", "h2", "h3", "h4", "h5", "h6", "p", "ul", "ol", "li",
            "a", "em", "strong", "b", "i", "u", "br", "img", "figure", "figcaption", "div",
            "table", "thead", "tbody", "tr", "th", "td", "colgroup", "col",
            "small", "sub", "sup", "picture", "source"
        };

        var allowedAttributes = new Dictionary<string, string[]>
        {
            { "a", ["href"] },
            { "img", ["src", "alt", "class", "width", "height", "style"] },
            { "figure", ["class"] },
            { "picture", [] },
            { "source", ["srcset", "type", "media", "sizes"] },
            { "th", ["colspan", "rowspan"] },
            { "td", ["colspan", "rowspan"] },
            { "colgroup", ["span"] },
            { "col", ["span", "style"] },
            { "div", ["class"] } // keep class to support placeholder substitution after cleaning
        };

        return HtmlTagRegex().Replace(html, match =>
        {
            var tag = match.Value;
            var tagNameMatch = TagNameExtractionRegex().Match(tag);

            if (!tagNameMatch.Success)
            {
                return preserveRejectedTags ? WebUtility.HtmlEncode(tag) : "";
            }

            var tagName = tagNameMatch.Groups["tagname"].Value.ToLower();

            // Remove disallowed tags completely (content will remain as text)
            if (!allowedTags.Contains(tagName))
            {
                return preserveRejectedTags ? WebUtility.HtmlEncode(tag) : "";
            }

            // Keep allowed tags, clean attributes if needed
            if (!allowedAttributes.TryGetValue(tagName, out var allowedTagAttributes))
            {
                return $"<{(tag.StartsWith("</") ? "/" : "")}{tagName}>";
            }

            // Clean attributes for tags that can have them
            if (tag.StartsWith("</"))
            {
                return tag;
            }

            var attrs = AttributeRegex().Matches(tag)
                .Where(m => allowedTagAttributes.Contains(m.Groups["name"].Value.ToLower()))
                .Select(m => m.Value);

            return attrs.Any()
                ? $"<{tagName} {string.Join(" ", attrs)}>"
                : $"<{tagName}>";
        });
    }

    /// <summary>
    /// Adds loading="lazy" attribute to all img tags for better performance.
    /// </summary>
    /// <param name="html">HTML content to optimize</param>
    /// <returns>HTML with lazy loading added to all images</returns>
    public static string AddLazyLoadingToImages(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        return ImageTagRegex().Replace(html, match =>
        {
            var imgTag = match.Value;

            // Insert loading="lazy" before the closing >
            var insertIndex = imgTag.LastIndexOf('>');
            if (insertIndex > 0)
            {
                return imgTag.Insert(insertIndex, " loading=\"lazy\"");
            }

            return imgTag;
        });
    }

    /// <summary>
    /// Strips all HTML tags and decodes HTML entities, returning plain text only.
    /// </summary>
    /// <param name="html">The HTML snippet to strip</param>
    /// <returns>Plain text with all tags removed</returns>
    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        // Replace tags with spaces
        var withoutTags = HtmlTagRegex().Replace(html, " ");

        // Decode entities (&nbsp;, &amp;, etc.)
        withoutTags = WebUtility.HtmlDecode(withoutTags);

        // Collapse multiple spaces
        return WhitespaceRegex().Replace(withoutTags, " ").Trim();
    }

    [GeneratedRegex(@"<img[^>]*>")]
    private static partial Regex ImageTagRegex(); // Matches all img tags

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex(); // Matches all HTML tags (opening, closing, self-closing)

    [GeneratedRegex(@"</?(?<tagname>\w+)")]
    private static partial Regex TagNameExtractionRegex(); // Extracts tag name from any tag

    [GeneratedRegex(@"(?<name>\w+)=[""'](?<value>[^""']*)[""']")]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}