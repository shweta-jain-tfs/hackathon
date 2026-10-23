namespace Pharos.Common.Web.Components.ChapterNavigation;
public sealed class ChapterNode
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Href { get; init; }
    public List<ChapterNode> Children { get; init; } = new();
    public bool HasChildren => Children.Count > 0;
}