using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels.Converters;

internal class ActionButtonConverter(IStringLocalizer<Translations> loc) : IComponentConverter
{
    public Type Type => typeof(ActionButton);

    public bool TryConvert(SettingItemBase item, IConverter converter, ISettingsDataService<SettingItemBase> dataService, out SettingsViewModelBase? result)
    {
        if (item is not ActionButton actionButton)
        {
            result = null;
            return false;
        }

        result = new ActionButtonViewModel(loc)
        {
            Model = actionButton
        };

        return true;
    }
}