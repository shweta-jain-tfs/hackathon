using Pharos.Client.Common.Models.HelpContent;

namespace Pharos.Client.Common.App.Interfaces;

public interface IHelpContentService
{
    Task<Dictionary<string, (string rootChapterId, List<string> path)>> GetChapterStructure();
    Task<List<HelpContentMaintenanceActivity>> GetMaintenanceActivities();
    Task<HelpContentManual> GetManual();
    Task<Dictionary<string, List<ChapterSearchIndex>>> GetManualSearchIndex();
    Task<HelpContentTaskCard> GetTaskCardHelp(int id);
}