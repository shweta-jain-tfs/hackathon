using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels.Converters;

internal class GroupConverter(IStringLocalizer<Translations> loc) : IComponentConverter
{
    public Type Type => typeof(Group);

    public bool TryConvert(SettingItemBase item, IConverter converter, ISettingsDataService<SettingItemBase> dataService, out SettingsViewModelBase? result)
    {
        if (item is not Group group)
        {
            result = null;
            return false;
        }

        // Populate DynamicDropdown choices
        ConverterHelper.PopulateDynamicDropdownChoices(group, dataService);

        var items = new List<SettingsViewModelBase>();
        if (group.Children.Count != 0)
        {
            items.AddRange(group.Children.Select(converter.Convert).ToList());
        }

        var groupViewModel = new GroupViewModel(loc)
        {
            Model = group,
            Items = items,
        };

        result = groupViewModel;
        return true;
    }
}