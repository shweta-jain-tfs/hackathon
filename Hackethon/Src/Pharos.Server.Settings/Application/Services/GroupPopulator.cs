using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;
internal class GroupPopulator : ISettingsComponentPopulator
{
    public void Populate(SettingItemBase component, in Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages)
    {
        if (component is not Group group)
        {
            return;
        }
        var dummyPage = new Page();
        var parentPageKey = string.Join(':', group.Key.Split(':').SkipLast(1));
        dummyPage.Key = parentPageKey;
        dummyPage.Url = group.Url;
        populatedPages.TryAdd(group.Key, dummyPage);
        foreach (var child in group.Children)
        {
            var populator = PopulatorFactory.GetPopulator(child);
            populator?.Populate(child, settings, ref populatedPages);
        }
    }
}
