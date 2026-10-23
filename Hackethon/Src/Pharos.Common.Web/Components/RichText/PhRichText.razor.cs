using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.RichText;

/// <summary>
/// Wrapper component for styling rich text content like manual content or context help.
/// Provides consistent typography, spacing, and formatting for HTML content including headings,
/// paragraphs, lists, tables, and images. Supports compact mode for context help and automatic
/// heading numbering for structured documentation.
/// Use <see cref="HtmlCleaner.ToCleanRichText(string?)"/> to sanitize HTML content.
/// </summary>
public partial class PhRichText
{
    /// <summary>
    /// HTML content to render. Use <see cref="HtmlCleaner.ToCleanRichText(string?)"/> for sanitized content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Use context help styling with smaller text and reduced spacing.
    /// </summary>
    [Parameter]
    public bool ContextHelp { get; set; }

    /// <summary>
    /// Enable automatic numbering for headings (h2, h3, h4).
    /// </summary>
    [Parameter]
    public bool NumberHeadings { get; set; }

    private string CssClasses => new CssBuilder("ph-richtext")
        .AddClass("ph-richtext--context-help", ContextHelp)
        .AddClass("ph-richtext--heading-numbers", NumberHeadings)
        .Build();
}