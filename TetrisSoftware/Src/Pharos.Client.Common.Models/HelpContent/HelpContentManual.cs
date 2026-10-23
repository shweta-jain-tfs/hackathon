namespace Pharos.Client.Common.Models.HelpContent;

public class HelpContentManual
{
    public HelpContentManual(string id, List<Chapter> chapters)
    {
        Id = id;
        Chapters = chapters;
    }

    public string Id { get; }
    public List<Chapter> Chapters { get; }
}