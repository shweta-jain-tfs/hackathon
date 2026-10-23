using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels.Converters;

internal class PageListConverter(IStringLocalizer<Translations> loc) : IComponentConverter
{
    public Type Type => typeof(PageList);

    public bool TryConvert(SettingItemBase item, IConverter converter, ISettingsDataService<SettingItemBase> dataService, out SettingsViewModelBase? result)
    {
        if (item is not PageList pageList)
        {
            result = null;
            return false;
        }

        var pages = new List<PagePlaceholder>();
        var pageStructure = (PageViewModel)converter.Convert(pageList.PageStructure);
        if (pageList.PageEntries.Count != 0)
        {
            pages.AddRange(pageList.PageEntries.Select(e => new PagePlaceholder { Key = e.Key, Label = e.Label }));
        }

        result = CreatePageListViewModel(pageList, pages, pageStructure, dataService, converter);

        return true;
    }

    private PageListViewModel CreatePageListViewModel(PageList pageList, List<PagePlaceholder> pages, PageViewModel pageStructure, ISettingsDataService<SettingItemBase> dataService, IConverter converter)
    {
        var pageListViewModel = new PageListViewModel(dataService, converter, loc)
        {
            Model = pageList,
            Headers = pageList.ColumnHeaderLabels.ToList(),
            PageEntries = pages,
            PageStructure = pageStructure,
            CopyEnabled = pageList.HasCopyFunction,
            IsProviderManaged = pageList.IsProviderManaged,
        };

        if (pageList.PageEntries == null || pageList.PageEntries.Count == 0)
        {
            return pageListViewModel;
        }

        foreach (var pageEntry in pageList.PageEntries)
        {
            var tableRowViewModel = new TableRowViewModel(loc)
            {
                EntryKey = pageEntry.Key,
                Model = pageEntry
            };

            pageListViewModel.Values.Add(tableRowViewModel);

            var pageEntryKey = pageEntry.Key;
            var pageEntryColumns = pageList.Columns.Where(v => v.Value.Key.Contains(pageEntryKey)).ToList();

            AddProviderManagedEntryLabel(pageList, pageEntry, tableRowViewModel);

            // Add all other column row values
            foreach (var pageListColumn in pageEntryColumns)
            {
                if (pageList.IsProviderManaged && pageListColumn.Value.IsNameColumn)
                {
                    continue;
                }

                tableRowViewModel.Values.Add(new TableEntryViewModel(loc)
                {
                    Header = pageListColumn.ColumnHeaderLabel,
                    Value = pageListColumn.Value.Value,
                    Model = pageListColumn.Value
                });
            }
        }

        if (pageList?.DynamicDropdowns != null && pageList.DynamicDropdowns.Count > 0)
        {
            pageListViewModel.DynamicDropdowns = pageList.DynamicDropdowns.Select(converter.Convert).Cast<ChoicesViewModel>().ToList();
        }

        return pageListViewModel;
    }

    private void AddProviderManagedEntryLabel(PageList pageList, ChildPageItem pageEntry, TableRowViewModel tableRowViewModel)
    {
        if (!pageList.IsProviderManaged || pageList.ColumnHeaderLabels.Count == 0)
        {
            return;
        }

        tableRowViewModel.Values.Add(new TableEntryViewModel(loc)
        {
            Header = pageList.ColumnHeaderLabels.First(),
            Value = pageEntry.Label,
            Model = pageEntry
        });
    }

}
