using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Pharos.Common.Web.Components.HighlightedContent;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Help.Utilities;

/// <summary>
/// Renders help content, replacing placeholder elements with pre-rendered component markup.
/// Call <see cref="CreateTemplatesAsync"/> once before rendering chapters.
/// </summary>
internal static partial class HelpContentRenderer
{
    public const string ContentMarker = "__PH_PLACEHOLDER_CONTENT__";

    private static readonly Dictionary<HelpPlaceholderType, HighlightedContentType> _highlightTypes = new()
    {
        [HelpPlaceholderType.NoteHighlight] = HighlightedContentType.Info,
        [HelpPlaceholderType.CautionHighlight] = HighlightedContentType.Warning,
        [HelpPlaceholderType.WarningHighlight] = HighlightedContentType.Error,
    };

    /// <summary>
    /// Renders a single chapter's content as one MarkupString with cleaned HTML and replaced placeholders.
    /// </summary>
    /// <remarks>
    /// Placeholders are replaced with pre-rendered component markup instead of rendering components between
    /// split MarkupString fragments. The browser parses each fragment separately and auto-closes open elements,
    /// so a placeholder inside e.g. a table cell would break the table. bUnit doesn't catch this.
    /// </remarks>
    public static MarkupString RenderChapterContent(string? content, IReadOnlyDictionary<HelpPlaceholderType, string> templates)
    {
        if (string.IsNullOrEmpty(content))
        {
            return default;
        }

        var cleanedContent = HtmlCleaner.CleanAndOptimizeHtml(content);
        cleanedContent = ReplaceHighlightPlaceholders(cleanedContent, templates);

        return new MarkupString(cleanedContent);
    }

    /// <summary>
    /// Pre-renders each placeholder component once, with a marker in place of the child content.
    /// </summary>
    /// <remarks>
    /// Kept separate so chapter rendering stays synchronous and testable without an HtmlRenderer.
    /// </remarks>
    public static async Task<IReadOnlyDictionary<HelpPlaceholderType, string>> CreateTemplatesAsync(HtmlRenderer htmlRenderer)
    {
        var templates = new Dictionary<HelpPlaceholderType, string>();

        // Highlights
        foreach (var (placeholder, highlightType) in _highlightTypes)
        {
            templates[placeholder] = await RenderAsync<PhHighlightedContent>(htmlRenderer, new()
            {
                [nameof(PhHighlightedContent.Type)] = highlightType,
                [nameof(PhHighlightedContent.ChildContent)] = (RenderFragment)(b => b.AddMarkupContent(0, ContentMarker))
            });
        }

        return templates;
    }

    private static Task<string> RenderAsync<TComponent>(HtmlRenderer htmlRenderer, Dictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        return htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await htmlRenderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
            return root.ToHtmlString();
        });
    }

    private static string ReplaceHighlightPlaceholders(string html, IReadOnlyDictionary<HelpPlaceholderType, string> templates)
    {
        return HighlightPlaceholderRegex().Replace(html, match =>
        {
            if (!templates.TryGetValue(ParseHighlightType(match.Groups["type"].Value), out var template))
            {
                return match.Value; // fallback: keep the original element unchanged
            }

            var inner = match.Groups["content"].Value
                .Replace("<h4></h4>", string.Empty, StringComparison.OrdinalIgnoreCase); // remove unused rogue heading

            return template.Replace(ContentMarker, inner);
        });
    }

    [GeneratedRegex(
    @"<div\s+class=[""'](?<type>note|caution|warning)[""'][^>]*>(?<content>.*?)</div>",
    RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HighlightPlaceholderRegex();

    private static HelpPlaceholderType ParseHighlightType(string highlightClass)
    {
        return highlightClass.ToLower() switch
        {
            "caution" => HelpPlaceholderType.CautionHighlight,
            "warning" => HelpPlaceholderType.WarningHighlight,
            _ => HelpPlaceholderType.NoteHighlight
        };
    }
}