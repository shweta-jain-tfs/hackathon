namespace Pharos.Common.Web.Components.Breadcrumb;

public class BreadcrumbItem
{
    public string Text { get; }
    public string Href { get; }
    public string? SettingKey { get; set; }
    public Action? Action { get; set; }

    public BreadcrumbItem(string text, string href, string? settingKey = null, Action? action = null)
    {
        Text = text;
        Href = href;
        SettingKey = settingKey;
        Action = action;
    }
}