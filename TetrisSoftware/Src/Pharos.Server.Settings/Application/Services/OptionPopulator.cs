using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;
internal class OptionPopulator : ISettingsComponentPopulator
{
    public void Populate(SettingItemBase component, in Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages)
    {
        if (component is Option option && settings.TryGetValue(option.Key, out var value))
        {
            option.Value = value;
        }
    }
}
