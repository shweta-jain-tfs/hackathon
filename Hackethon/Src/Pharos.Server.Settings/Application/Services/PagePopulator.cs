using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;
internal class PagePopulator : ISettingsComponentPopulator
{
    public void Populate(SettingItemBase component, in Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages)
    {
        if (component is not Page page)
        {
            return;
        }

        populatedPages.TryAdd(page.Key, page);
        foreach (var child in page.Children)
        {
            PopulateChild(child, settings, ref populatedPages);
        }

        populatedPages.TryAdd(page.Key, page);
    }

    private static void PopulateChild(SettingsChildItem child, Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages)
    {
        var populator = PopulatorFactory.GetPopulator(child);
        populator?.Populate(child, settings, ref populatedPages);
    }
}
