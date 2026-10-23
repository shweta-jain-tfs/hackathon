using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Application.Metadata;

public class Group : SettingsChildItem, ISettingsParentItem
{
    public List<SettingsChildItem> Children { get; set; } = [];
    public string HelpUrl { get; set; } = "";

    public override SettingsChildItem DeepCopy(string settingsKeyPrefix, string parentNavigationPath)
    {
        var key = settingsKeyPrefix + PageHelper.SettingsKeyDeLimiter + PageHelper.GetSuffixKeyName(Key);
        var helpUrl = parentNavigationPath + PageHelper.SettingsKeyDeLimiter + Label;
        var labelRoot = parentNavigationPath.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter) + PageHelper.SettingsLabelDeLimiter + Label;

        var dependency = PageHelper.CopyDependency(Dependency, settingsKeyPrefix);

        return new Group
        {
            Key = key,
            HelpUrl = helpUrl,
            Label = Label,
            Url = labelRoot,
            Dependency = dependency,
            Permissions = Permissions,
            ExcludeFromImportExport = ExcludeFromImportExport,
            Children = Children.Select(c => c.DeepCopy(key, helpUrl)).ToList(),
            ShowOnlyInRndMode = ShowOnlyInRndMode
        };
    }
}