using System.Text.Json.Serialization;

namespace Pharos.Server.Settings.Application.Metadata;

[JsonDerivedType(typeof(Group), nameof(Group))]
[JsonDerivedType(typeof(Option), nameof(Option))]
[JsonDerivedType(typeof(PageList), nameof(PageList))]
[JsonDerivedType(typeof(PagePlaceholder), nameof(PagePlaceholder))]
[JsonDerivedType(typeof(Page), nameof(Page))]
[JsonDerivedType(typeof(ChildPageItem), nameof(ChildPageItem))]
[JsonDerivedType(typeof(ActionButton), nameof(ActionButton))]
public class SettingsChildItem : SettingItemBase
{
    public virtual SettingsChildItem DeepCopy(string settingsKeyPrefix, string parentNavigationPath)
    {
        throw new NotImplementedException("Subclass needs implementation!");
    }
}
