using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels.Converters;

internal class OptionConverter(IStringLocalizer<Translations> loc) : IComponentConverter
{
    public Type Type => typeof(Option);

    public bool TryConvert(SettingItemBase item, IConverter converter, ISettingsDataService<SettingItemBase> dataService, out SettingsViewModelBase? result)
    {
        if (item is not Option option)
        {
            result = null;
            return false;
        }

        result = OptionViewModelFactory.CreateOptionViewModel(option, loc, dataService);

        return true;
    }
}