namespace Pharos.Common.Web.Components.ChapterNavigation;

/// <summary>
/// Used internally by <see cref="PhChapterNav"/> to render individual chapter or section nodes.
/// </summary>
public partial class PhChapterNavNode
{
    /// <summary>
    /// The chapter or section node to render.
    /// </summary>
    [Parameter] public required ChapterNode Node { get; set; }

    /// <summary>
    /// The ID of the currently selected chapter.
    /// </summary>
    [Parameter] public string? CurrentId { get; set; }

    /// <summary>
    /// Shared state for expanded/collapsed chapters.
    /// </summary>
    [Parameter] public required Dictionary<string, bool> ExpandedState { get; set; }

    /// <summary>
    /// If true, disables normal link navigation.
    /// </summary>
    [Parameter] public bool PreventNavigation { get; set; }

    /// <summary>
    /// The nesting level (0 = top-level/document).
    /// </summary>
    [Parameter] public int Level { get; set; } = 0;

    /// <summary>
    /// Called when a chapter is clicked.
    /// </summary>
    [Parameter] public EventCallback<string> OnNavigate { get; set; }

    /// <summary>
    /// Called when a chapter is expanded or collapsed.
    /// </summary>
    [Parameter] public EventCallback<(string nodeId, bool expanded)> OnToggle { get; set; }

    private bool IsExpanded => ExpandedState.TryGetValue(Node.Id, out var expanded) && expanded;

    private async Task Toggle()
    {
        var newExpanded = !IsExpanded;
        await OnToggle.InvokeAsync((Node.Id, newExpanded));
    }

    private async Task Navigate()
    {
        // If this node has children, expand it when navigating to it
        if (Node.HasChildren && !IsExpanded)
        {
            await OnToggle.InvokeAsync((Node.Id, true));
        }

        await OnNavigate.InvokeAsync(Node.Id);
    }

    private string? GetAriaCurrent()
    {
        if (Node.Id != CurrentId)
        {
            return null;
        }

        return Level == 0 ? "page" : "location";
    }
}