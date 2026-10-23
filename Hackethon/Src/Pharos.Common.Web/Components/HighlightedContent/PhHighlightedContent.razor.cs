using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.HighlightedContent;

/// <summary>
/// Highlights important content sections in rich text with a status icon and colored background.
/// Use this component to draw attention to critical information, warnings, tips, or errors within documentation.
/// The icon and color scheme are determined by the selected <see cref="Type"/> parameter.
/// </summary>
public partial class PhHighlightedContent
{
    /// <summary>
    /// The content to be highlighted. Can contain any HTML elements including text, lists, paragraphs, and other components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The type of highlighted content which determines the icon and color scheme.
    /// Defaults to <see cref="HighlightedContentType.Info"/>.
    /// </summary>
    [Parameter] public HighlightedContentType Type { get; set; } = HighlightedContentType.Info;

    private StatusIconType StatusIcon => Type switch
    {
        HighlightedContentType.Ok => StatusIconType.Ok,
        HighlightedContentType.Warning => StatusIconType.Warning,
        HighlightedContentType.Error => StatusIconType.Alarm,
        HighlightedContentType.Info or _ => StatusIconType.PendingAction
    };

    private string CssClasses => new CssBuilder("ph-highlighted-content")
        .AddClass("ph-highlighted-content--ok", Type == HighlightedContentType.Ok)
        .AddClass("ph-highlighted-content--info", Type == HighlightedContentType.Info)
        .AddClass("ph-highlighted-content--warning", Type == HighlightedContentType.Warning)
        .AddClass("ph-highlighted-content--error", Type == HighlightedContentType.Error)
        .Build();
}