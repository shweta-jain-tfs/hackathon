using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Interfaces;
internal interface ISettingsComponentPopulator
{
    void Populate(SettingItemBase component, in Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages);
}
