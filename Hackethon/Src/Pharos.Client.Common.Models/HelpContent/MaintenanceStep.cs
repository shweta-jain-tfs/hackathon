namespace Pharos.Client.Common.Models.HelpContent;

public class MaintenanceStep(string key, string navigationTitle, string? title, string? content)
{
    public string Key { get; } = key;
    public string NavigationTitle { get; } = navigationTitle;
    public string? Title { get; } = title;
    public string? Content { get; } = content;
}