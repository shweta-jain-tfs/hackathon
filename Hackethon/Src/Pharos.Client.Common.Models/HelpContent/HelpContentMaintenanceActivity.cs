namespace Pharos.Client.Common.Models.HelpContent;

public class HelpContentMaintenanceActivity
{
    public HelpContentMaintenanceActivity(string key, string title, List<MaintenanceStep> steps)
    {
        Key = key;
        Title = title;
        Steps = steps ?? [];
    }

    public string Key { get; }
    public string Title { get; }
    public List<MaintenanceStep> Steps { get; }
}