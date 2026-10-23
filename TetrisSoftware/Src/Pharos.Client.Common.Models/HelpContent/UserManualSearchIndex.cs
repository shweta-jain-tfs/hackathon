namespace Pharos.Client.Common.Models.HelpContent;

public record UserManualSearchIndex
{
    public Dictionary<string, List<ChapterSearchIndex>> SearchIndex { get; set; } = new();
}


public record ChapterSearchIndex
{
    public required string ChapterId { get; set; }
    public required string Title { get; set; }
}

