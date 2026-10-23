using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;
internal static class PopulatorFactory
{
    public static ISettingsComponentPopulator? GetPopulator(SettingItemBase component)
    {
        return component switch
        {
            Page => new PagePopulator(),
            Group => new GroupPopulator(),
            Option => new OptionPopulator(),
            PageList => new PageListPopulator(),
            _ => null
        };
    }
}
