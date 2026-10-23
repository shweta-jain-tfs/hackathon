using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels.Converters;

internal class PageConverter(IStringLocalizer<Translations> loc) : IComponentConverter
{
    public Type Type => typeof(Page);

    public bool TryConvert(SettingItemBase item, IConverter converter, ISettingsDataService<SettingItemBase> dataService, out SettingsViewModelBase? result)
    {
        if (item is not Page page)
        {
            result = null;
            return false;
        }

        // Populate DynamicDropdown choices
        ConverterHelper.PopulateDynamicDropdownChoices(page, dataService);

        var items = new List<SettingsViewModelBase>();
        if (page.Children.Count != 0)
        {
            items.AddRange(page.Children.Select(converter.Convert).ToList());
        }


        var pageViewModel = new PageViewModel(loc)
        {
            Items = items,
            Model = page
        };

        SetDependencies(pageViewModel);

        result = pageViewModel;
        return true;
    }

    private static void SetDependencies(PageViewModel page)
    {
        foreach (var item in page.Items)
        {
            var dependencyItems = new List<SettingsViewModelBase>();

            if (item.Dependency == null)
            {
                continue;
            }

            foreach (var dependency in item.Dependency)
            {
                if (!string.IsNullOrEmpty(dependency.MainOptionKey))
                {

                    if (item is GroupViewModel)
                    {
                        var dependencyItem = FindItemInGroup(page.Items, item);
                        if (dependencyItem != null)
                        {
                            dependencyItems.Add(dependencyItem);
                        }
                    }
                    else if (item is PageViewModel)
                    {
                        var dependencyItem = FindItemInGroup(page.Items, item);
                        if (dependencyItem != null)
                        {
                            dependencyItems.Add(dependencyItem);
                        }
                    }
                    else if (item is OptionViewModelBase)
                    {
                        var dependencyItem = page.Items.FirstOrDefault(i =>
                            dependency.MainOptionKey == i.Key && item.Dependency != null);
                        if (dependencyItem != null)
                        {
                            dependencyItems.Add(dependencyItem);
                        }
                    }
                }
            }

            item.DependencyViewModel = dependencyItems;

            if (item is GroupViewModel groupViewModel)
            {
                SetDependencies(groupViewModel, page.Items);
            }
        }
    }

    private static void SetDependencies(GroupViewModel group, List<SettingsViewModelBase> pageItems)
    {
        foreach (var item in group.Items)
        {
            var dependencyItems = new List<SettingsViewModelBase>();

            if (item.Dependency == null)
            {
                continue;
            }

            foreach (var dependency in item.Dependency)
            {
                if (!string.IsNullOrEmpty(dependency.MainOptionKey))
                {
                    var dependencyItem = FindItemInGroup(pageItems, item);
                    if (dependencyItem != null)
                    {
                        dependencyItems.Add(dependencyItem);
                    }
                }
            }

            item.DependencyViewModel = dependencyItems;
        }
    }
    private static SettingsViewModelBase? FindItemInGroup(List<SettingsViewModelBase> items, SettingsViewModelBase viewModel)
    {
        foreach (var item in items)
        {
            if (item is GroupViewModel groupViewModel)
            {
                var nestedItem = FindItemInGroup(groupViewModel.Items, viewModel);
                if (nestedItem != null)
                {
                    return nestedItem;
                }
            }

            if (viewModel.Dependency != null)
            {
                foreach (var dependency in viewModel.Dependency)
                {
                    if (dependency.MainOptionKey != null && item.Key != null && dependency.MainOptionKey.Contains(item.Key))
                    {
                        return item;
                    }
                }
            }
        }

        return null;
    }
}