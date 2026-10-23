using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.ChapterNavigation;

/// <summary>
/// Interactive chapter navigation component for hierarchical document structures.
/// Top-level nodes represent documents, with nested sub-nodes representing chapters and sections.
/// Supports expand/collapse functionality and automatic path expansion to current chapter.
/// </summary>
public partial class PhChapterNav
{
    /// <summary>
    /// The hierarchical chapter structure to display in the navigation.
    /// Top-level ChapterNodes represent documents, with nested children representing
    /// chapters and sub-sections within those documents.
    /// </summary>
    [Parameter] public required IReadOnlyList<ChapterNode> Items { get; set; }

    /// <summary>
    /// The ID of the currently active chapter. Used to support deep linking to a specific chapter
    /// and automatically expand the path to show the current location when scrolling or navigating.
    /// When set, the component will automatically expand parent nodes to reveal this chapter.
    /// Can reference any node ID at any level (document, chapter, or sub-section).
    /// </summary>
    [Parameter] public string? CurrentChapterId { get; set; }

    /// <summary>
    /// Event callback invoked when a user clicks on a chapter link.
    /// Receives the chapter ID that was clicked, allowing the parent component
    /// to handle navigation logic (e.g., updating URL, loading content, etc.).
    /// The returned chapter ID should typically be passed back to <see cref="CurrentChapterId"/>
    /// to update the component's active state and expand the correct path.
    /// </summary>
    [Parameter] public EventCallback<string> OnNavigate { get; set; }

    /// <summary>
    /// When true, prevents default browser navigation when clicking chapter links.
    /// Useful for UI testing in Storybook and when the parent component needs
    /// to control navigation behavior instead of relying on standard anchor tag navigation.
    /// </summary>
    [Parameter] public bool PreventNavigation { get; set; }

    /// <summary>
    /// When true, displays the navigation with a subtle background color and adds padding.
    /// </summary>
    [Parameter] public bool WithBackground { get; set; }

    private readonly Dictionary<string, bool> _expandedState = new();

    private string CssClasses => new CssBuilder("ph-chapter-nav")
        .AddClass("ph-chapter-nav--with-background", WithBackground)
        .Build();

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(CurrentChapterId))
        {
            // First, collapse all top-level nodes
            foreach (var topLevelNode in Items)
            {
                _expandedState[topLevelNode.Id] = false;
            }

            // Then expand the path to current chapter (this will re-expand the correct top-level node)
            ExpandPathTo(CurrentChapterId, Items);
        }
    }

    private bool ExpandPathTo(string id, IEnumerable<ChapterNode> nodes)
    {
        foreach (var n in nodes)
        {
            if (n.Id == id)
            {
                if (n.HasChildren)
                {
                    _expandedState[n.Id] = true;
                }

                return true;
            }

            if (n.Children.Count > 0 && ExpandPathTo(id, n.Children))
            {
                _expandedState[n.Id] = true; // expand ancestors
                return true;
            }
        }

        return false;
    }

    private void HandleToggle((string nodeId, bool expanded) toggleData)
    {
        _expandedState[toggleData.nodeId] = toggleData.expanded;
    }

    private async Task HandleNavigate(string nodeId)
    {
        // Note: you're expected to pass the selected nodeId back in as CurrentChapterId
        await OnNavigate.InvokeAsync(nodeId);
    }
}