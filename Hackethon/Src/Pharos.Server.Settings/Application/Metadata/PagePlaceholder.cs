using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Application.Metadata;
public class PagePlaceholder : ChildPageItem
{
    public List<Option> Options { get; set; } = [];

    public override SettingsChildItem DeepCopy(string settingsKeyPrefix, string parentNavigationPath)
    {
        return new PagePlaceholder
        {
            Key = Key = settingsKeyPrefix + PageHelper.SettingsKeyDeLimiter + PageHelper.GetSuffixKeyName(Key),
            Label = Label,
            Url = Url,
            ExcludeFromImportExport = ExcludeFromImportExport,
            Options = Options
        };
    }
}
