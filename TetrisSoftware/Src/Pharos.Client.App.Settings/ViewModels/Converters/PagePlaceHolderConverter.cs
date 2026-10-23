using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels.Converters;
internal class PagePlaceHolderConverter(IStringLocalizer<Translations> loc) : IComponentConverter
{
    public Type Type => typeof(PagePlaceholder);
    public bool TryConvert(SettingItemBase item, IConverter converter, ISettingsDataService<SettingItemBase> dataService, out SettingsViewModelBase? result)
    {
        if (item is not PagePlaceholder pagePlaceholder)
        {
            result = null;
            return false;
        }

        result = new PageViewModel(loc)
        {
            Model = pagePlaceholder,
        };

        return true;
    }
}
