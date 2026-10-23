using System.Globalization;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;
internal class PageListPopulator : ISettingsComponentPopulator
{
    public void Populate(SettingItemBase component, in Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages)
    {
        if (component is not PageList pageList)
        {
            return;
        }

        if (pageList.PageStructure is not Page pageStructure)
        {
            throw new InvalidOperationException($"PageList does not have a page structure!");
        }

        populatedPages.TryAdd(pageStructure.Key, pageStructure);

        PopulateExistingEntries(pageList, pageStructure, settings, ref populatedPages);
        AddDbEntries(pageList, pageStructure, settings, ref populatedPages);
    }

    private static void PopulateExistingEntries(PageList pageList, Page pageStructure, Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages)
    {
        // Update existing items in list (from initialization in class definition)
        foreach (var pageItem in pageList.PageEntries.OfType<Page>())
        {
            if (populatedPages.TryGetValue(pageItem.Key, out var page))
            {
                PopulatePage(page, settings, ref populatedPages);
                PageListColumnHelper.ExtractAndAddColumns(pageList, page);
            }
            else
            {
                // Add initial PageListEntries and populate them
                populatedPages.TryAdd(pageItem.Key, pageItem);
                PopulatePage(pageItem, settings, ref populatedPages);

                // Update the Name property of initial PageListEntries
                var pageLabel = GetEntryLabel(pageList, pageItem.Key, pageItem.Label, pageItem.Children);
                var entryIndex = TryGetListEntryIndex(pageItem.Key, pageList.Key)
                    ?? throw new InvalidOperationException($"Cannot extract index from key '{pageItem.Key}'.");
                var (_, url) = BuildEntryPaths(pageStructure.Url, entryIndex);
                pageItem.Url = url;
                pageItem.Label = pageLabel;
                PageListColumnHelper.ExtractAndAddColumns(pageList, pageItem);
            }
        }
    }

    private static void AddDbEntries(PageList pageList, Page pageStructure, Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages)
    {
        // Create entries in list that have been added by a user
        var pageListSettings = settings
            .Where(s => s.Key.StartsWith(pageList.Key, StringComparison.Ordinal))
            .ToDictionary(k => k.Key, v => v.Value);

        var existingEntryIndices = pageList.PageEntries
            .Select(entry => TryGetListEntryIndex(entry.Key, pageList.Key))
            .Where(index => index.HasValue)
            .Select(index => index!.Value)
            .ToHashSet();

        var indicesToAdd = pageListSettings.Keys
            .Select(key => TryGetListEntryIndex(key, pageList.Key))
            .Where(index => index.HasValue)
            .Select(index => index!.Value)
            .Where(index => !existingEntryIndices.Contains(index))
            .Distinct()
            .OrderBy(index => index)
            .ToList();

        foreach (var index in indicesToAdd)
        {
            var newKey = pageStructure.Key + PageHelper.SettingsKeyDeLimiter + index;
            var pageLabel = GetDbOrDefaultEntryLabel(pageList, pageListSettings, newKey, pageStructure);
            var (entryPath, url) = BuildEntryPaths(pageStructure.Url, index);

            var item = new Page
            {
                Key = newKey,
                Header = pageStructure.Header,
                Url = url,
                Label = pageLabel!,
                Dependency = pageStructure.Dependency,
                Children = pageStructure.Children.Select(c => c.DeepCopy(newKey, entryPath)).ToList()
            };

            PopulatePage(item, settings, ref populatedPages);
            PageListColumnHelper.ExtractAndAddColumns(pageList, item);

            pageList.PageEntries.Add(item);
        }
    }

    private static string GetDbOrDefaultEntryLabel(PageList pageList, Dictionary<string, string?> tableDbSettings, string entryKey, Page pageStructure)
    {
        var labelKey = entryKey + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName;
        if (tableDbSettings.TryGetValue(labelKey, out var pageLabel) && !string.IsNullOrEmpty(pageLabel))
        {
            return pageLabel;
        }

        var keyNoIndex = pageStructure.Key + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName;
        var defaultNameOption = pageStructure.Children.OfType<Option>().FirstOrDefault(c => c.Key == keyNoIndex);
        if (defaultNameOption != null)
        {
            return defaultNameOption.Value
                ?? throw new ArgumentException($"No value found for '{PageList.RequiredTablePropertyName}' in page structure '{pageStructure.Key}'.");
        }

        return GetProviderManagedFallbackEntryLabel(pageList, tableDbSettings, entryKey)
            ?? throw new ArgumentException($"No value found for '{PageList.RequiredTablePropertyName}' in page structure '{pageStructure.Key}'.");
    }

    private static string GetEntryLabel(PageList pageList, string entryKey, string entryLabel, IEnumerable<SettingsChildItem> children)
    {
        var keyNoIndex = entryKey + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName;
        var nameOption = children.OfType<Option>().FirstOrDefault(c => c.Key == keyNoIndex);
        if (nameOption != null)
        {
            return nameOption.Value
                ?? throw new ArgumentException($"No value found for initial PageListEntry, {entryLabel}");
        }

        return GetProviderManagedFallbackEntryLabel(pageList, entryKey, children)
            ?? throw new ArgumentException($"No value found for initial PageListEntry, {entryLabel}");
    }

    private static string? GetProviderManagedFallbackEntryLabel(PageList pageList, Dictionary<string, string?> tableDbSettings, string entryKey)
    {
        if (!pageList.IsProviderManaged || string.IsNullOrWhiteSpace(pageList.ProviderKeyPropertyName))
        {
            return null;
        }

        var entryKeySetting = entryKey + PageHelper.SettingsKeyDeLimiter + pageList.ProviderKeyPropertyName;
        return tableDbSettings.TryGetValue(entryKeySetting, out var entryKeyValue) && !string.IsNullOrWhiteSpace(entryKeyValue)
            ? entryKeyValue
            : null;
    }

    private static string? GetProviderManagedFallbackEntryLabel(PageList pageList, string entryKey, IEnumerable<SettingsChildItem> children)
    {
        if (!pageList.IsProviderManaged || string.IsNullOrWhiteSpace(pageList.ProviderKeyPropertyName))
        {
            return null;
        }

        var entryKeyOptionKey = entryKey + PageHelper.SettingsKeyDeLimiter + pageList.ProviderKeyPropertyName;
        return children.OfType<Option>().FirstOrDefault(c => c.Key == entryKeyOptionKey)?.Value;
    }

    private static (string entryPath, string url) BuildEntryPaths(string pageStructureUrl, int entryIndex)
    {
        var indexStr = entryIndex.ToString(CultureInfo.InvariantCulture);
        var segments = pageStructureUrl.Split(PageHelper.SettingsLabelDeLimiter).SkipLast(1);
        var baseUrl = string.Join(PageHelper.SettingsKeyDeLimiter, segments);
        var entryPath = baseUrl + PageHelper.SettingsKeyDeLimiter + indexStr;
        var url = baseUrl.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter) + PageHelper.SettingsLabelDeLimiter + indexStr;
        return (entryPath, url);
    }

    private static void PopulatePage(Page page, Dictionary<string, string?> settings, ref Dictionary<string, Page> populatedPages)
    {
        var populator = PopulatorFactory.GetPopulator(page);
        populator?.Populate(page, settings, ref populatedPages);
    }

    private static int? TryGetListEntryIndex(string key, string parentKey)
    {
        var afterParentStart = parentKey.Length + 1; // including ":"
        if (key.Length <= afterParentStart)
        {
            return null;
        }

        // Handle page-entry keys of form "parent:index" directly.
        // Example: "Samples:TubeTypes:TubeTypeList:5"
        if (int.TryParse(key[afterParentStart..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var directIndex))
        {
            return directIndex;
        }

        var afterParent = key.Substring(afterParentStart);
        var delimiterIndex = afterParent.IndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.InvariantCulture);
        if (delimiterIndex == -1)
        {
            return null;
        }

        var indexStr = afterParent[..delimiterIndex];
        if (!int.TryParse(indexStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return null;
        }

        return index;
    }
}
