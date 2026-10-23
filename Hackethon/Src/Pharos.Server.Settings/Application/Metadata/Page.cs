using System.ComponentModel.DataAnnotations;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Application.Metadata;

public class Page : ChildPageItem, ISettingsParentItem
{
    [MaxLength(128)] public string Header { get; set; } = "";

    public List<SettingsChildItem> Children { get; set; } = [];

    public override SettingsChildItem DeepCopy(string settingsKeyPrefix, string parentNavigationPath)
    {
        var key = settingsKeyPrefix + PageHelper.SettingsKeyDeLimiter + PageHelper.GetSuffixKeyName(Key);
        var newKeyPathWithoutIndex = parentNavigationPath + PageHelper.SettingsKeyDeLimiter + Label;
        var labelRoot = parentNavigationPath.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter) + PageHelper.SettingsLabelDeLimiter + Label;

        var dependency = PageHelper.CopyDependency(Dependency, settingsKeyPrefix);

        return new Page
        {
            Key = key,
            Header = Header,
            Label = Label,
            Url = labelRoot,
            Dependency = dependency,
            Permissions = Permissions,
            ExcludeFromImportExport = ExcludeFromImportExport,
            Children = Children.Select(c => c.DeepCopy(key, newKeyPathWithoutIndex)).ToList(),
            ShowOnlyInRndMode = ShowOnlyInRndMode
        };
    }
}