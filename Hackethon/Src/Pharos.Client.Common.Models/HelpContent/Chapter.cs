namespace Pharos.Client.Common.Models.HelpContent;

public class Chapter(string id, string title, string content, List<Chapter> chapters)
{
    public string Id { get; } = id;
    public string? Title { get; } = title;
    public string? Content { get; set; } = content;
    public List<Chapter> Chapters { get; } = chapters;
}
