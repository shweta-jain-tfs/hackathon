using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Links;


/// <summary>
/// Hyperlink component. Inherits the surrounding text size unless a size is specified.
/// </summary>
public partial class PhHrefLink
{
    /// <summary>
    /// Disables the link and applies disabled styling.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// The target URL for the link.
    /// </summary>
    [Parameter] public string? Href { get; set; }
    [Parameter] public bool ShowUnderline { get; set; }

    /// <summary>
    /// Raised when the link is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Content rendered inside the link.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Visual size of the link; defaults to inheriting the surrounding text size.
    /// </summary>
    [Parameter] public LinkSize LinkSize { get; set; } = LinkSize.None;

    protected string Classname => new CssBuilder("ph-link")
        .AddClass("ph-link--disabled", Disabled)
        .AddClass("ph-link--underline", ShowUnderline)
        .AddClass("t-h1", LinkSize == LinkSize.H1)
        .AddClass("t-h2", LinkSize == LinkSize.H2)
        .AddClass("t-h3", LinkSize == LinkSize.H3)
        .AddClass("t-body1", LinkSize == LinkSize.Body1)
        .Build();

    protected async Task OnClickHandler(MouseEventArgs ev)
    {
        if (Disabled)
        {
            return;
        }

        await OnClick.InvokeAsync(ev);
    }
}

public enum LinkSize
{
    None,
    H1,
    H2,
    H3,
    Body1
}