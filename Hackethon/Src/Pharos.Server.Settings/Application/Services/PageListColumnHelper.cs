using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;

internal static class PageListColumnHelper
{
    public static void Rebuild(PageList pageList)
    {
        pageList.ColumnHeaderLabels.Clear();
        pageList.Columns.Clear();

        if (pageList.PageStructure is Page pageStructure)
        {
            if (pageList.IsProviderManaged)
            {
                pageList.ColumnHeaderLabels.Add(GetProviderManagedDisplayNameColumnHeader(pageStructure));
            }

            pageList.ColumnHeaderLabels.AddRange(GetColumnOptions(pageStructure)
                .Where(o => !(pageList.IsProviderManaged && o.IsNameColumn))
                .Select(o => o.Label));
        }

        foreach (var page in pageList.PageEntries.OfType<Page>())
        {
            ExtractAndAddColumns(pageList, page);
        }
    }

    public static IReadOnlyList<Option> GetColumnOptions(ISettingsParentItem parent)
    {
        var options = new List<Option>();
        AddColumnOptions(parent.Children, options);
        return options;
    }

    public static void ExtractAndAddColumns(PageList pageList, Page page)
    {
        foreach (var columnOption in GetColumnOptions(page))
        {
            if (pageList.IsProviderManaged && columnOption.IsNameColumn)
            {
                continue;
            }

            pageList.Columns.Add(new Row
            {
                ColumnHeaderLabel = columnOption.Label,
                Value = columnOption
            });
        }
    }

    private static void AddColumnOptions(IEnumerable<SettingsChildItem> children, List<Option> options)
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case Option { IsColumnHeader: true } option:
                    options.Add(option);
                    break;
                case Option { IsNameColumn: true } option:
                    options.Add(option);
                    break;
                case PageList:
                    break;
                case ISettingsParentItem parent:
                    AddColumnOptions(parent.Children, options);
                    break;
            }
        }
    }

    private static string GetProviderManagedDisplayNameColumnHeader(Page pageStructure)
    {
        return GetColumnOptions(pageStructure).FirstOrDefault(o => o.IsNameColumn)?.Label
            ?? PageList.ProviderManagedDisplayNameColumnHeader;
    }
}
