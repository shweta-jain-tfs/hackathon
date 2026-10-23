namespace Pharos.Client.Common.Models.HelpContent;

public class HelpContentTaskCard
{
    public HelpContentTaskCard(int id, string content, int chapterId)
    {
        Id = id;
        Content = content;
        ChapterId = chapterId;
    }

    public int Id { get; set; }
    public string Content { get; set; }
    public int ChapterId { get; set; }
}

