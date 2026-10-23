namespace Pharos.Common.Web.Components.Links;

/// <summary>
/// Blazor component that renders a list of links separated by a configurable separator.
/// </summary>
/// <remarks>
/// Each item in <see cref="Links"/> can specify either a navigable <see cref="LinkItem.Href"/> or an asynchronous
/// click handler via <see cref="LinkItem.OnClick"/>. The rendering logic (in the accompanying .razor file)
/// determines how these are displayed.
/// </remarks>
public partial class PhLinkList
{
    /// <summary>
    /// Gets or sets the collection of link items to render.
    /// </summary>
    /// <remarks>
    /// Must be provided by the consumer. The list is iterated in order and a separator is rendered between
    /// items except after the final item.
    /// </remarks>
    [Parameter] public List<LinkItem> Links { get; set; } = default!;

    /// <summary>
    /// Gets or sets the separator string rendered between link items.
    /// </summary>
    /// <value>Defaults to <c>", "</c>.</value>
    [Parameter] public string Separator { get; set; } = ", ";

    /// <summary>
    /// Returns the separator if the specified index is not the last item in the list; otherwise an empty string.
    /// </summary>
    /// <param name="index">Zero-based index of the current link item.</param>
    /// <returns>The separator for non-final items; otherwise <see cref="string.Empty"/>.</returns>
    private string GetSeparator(int index)
    {
        if (index < Links.Count - 1)
        {
            return Separator;
        }

        return string.Empty;
    }
}

/// <summary>
/// Represents a single link item that can be rendered by <see cref="PhLinkList"/>.
/// </summary>
/// <remarks>
/// Provide either <see cref="Href"/> for navigation or <see cref="OnClick"/> for an action.
/// If both are set, rendering logic should define precedence (typically <see cref="OnClick"/>).
/// </remarks>
public class LinkItem
{
    /// <summary>
    /// Gets or sets the display text for the link.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Gets or sets the target URL for navigation when the link is clicked.
    /// </summary>
    /// <remarks>Leave <c>null</c> if using <see cref="OnClick"/> exclusively.</remarks>
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets an optional asynchronous callback executed when the link is clicked.
    /// </summary>
    /// <remarks>
    /// Use this for in-page actions or logic that should not trigger navigation.
    /// Ignored if the rendering chooses to prioritize <see cref="Href"/>.
    /// </remarks>
    public Func<Task>? OnClick { get; set; }
}