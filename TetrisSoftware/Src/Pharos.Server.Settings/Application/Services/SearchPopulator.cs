using System.Globalization;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services
{
    internal static class SearchPopulator
    {
        public static Page PopulateTopLevelPage(Page page, in Dictionary<string, string?> settings)
        {
            foreach (var child in page.Children)
            {
                PopulateChild(child, settings);
            }

            return page;
        }

        private static void PopulatePage(Page page, Dictionary<string, string?> settings)
        {
            foreach (var child in page.Children)
            {
                PopulateChild(child, settings);
            }
        }

        private static void PopulateChild(SettingsChildItem child, Dictionary<string, string?> settings)
        {
            switch(child)
            {
                case Page page:
                    PopulatePage(page, settings);
                    break;
                case Group group:
                    PopulateGroup(group, settings);
                    break;
                case Option option:
                    PopulateOption(option, settings);
                    break;
                case PageList pageList:
                    PopulatePageList(pageList, settings);
                    break;
            };
        }


        private static void PopulateGroup(Group group, in Dictionary<string, string?> settings)
        {
            var dummyPage = new Page();
            var parentPageKey = string.Join(':', group.Key.Split(':').SkipLast(1));
            dummyPage.Key = parentPageKey;
            dummyPage.Url = group.Url;
            foreach (var child in group.Children)
            {
                PopulateChild(child, settings);
            }
        }


        private static void PopulateOption(Option option, in Dictionary<string, string?> settings)
        {
            if (settings.TryGetValue(option.Key, out var value))
            {
                option.Value = value;
            }
        }



        private static void PopulatePageList(PageList pageList, in Dictionary<string, string?> settings)
        {
            if (pageList.PageStructure == null)
            {
                throw new InvalidOperationException($"PageList does not have a page structure!");
            }

            // Update existing items in list (from initialization in class definition)
            foreach (var pageItem in pageList.PageEntries)
            {

                // Add initial PageListEntries and populate them
                PopulatePage((Page)pageItem, settings);
                if (pageList.PageStructure is not Page pageStructure)
                {
                    throw new InvalidOperationException("PageStructure is not of type Page.");
                }

                // Update the Name property of initial PageListEntries
                var initialPage = (Page)pageItem;
                var segments = initialPage.Url.Split(PageHelper.SettingsLabelDeLimiter).SkipLast(1);
                var baseUrl = string.Join(PageHelper.SettingsKeyDeLimiter, segments);
                var keyNoIndex = initialPage.Key + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName;
                var pageLabel = initialPage.Children.OfType<Option>().FirstOrDefault(c => c.Key == keyNoIndex)?.Value
                    ?? GetProviderManagedFallbackEntryLabel(pageList, initialPage.Key, initialPage.Children)
                    ?? throw new ArgumentException($"No value found for initial PageListEntry, {pageItem.Label}");
                var keyPathWithoutIndex = baseUrl + PageHelper.SettingsKeyDeLimiter + pageLabel;
                var url = baseUrl.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter) + PageHelper.SettingsLabelDeLimiter + pageLabel;
                initialPage.Label = pageLabel;
                initialPage.Url = url;
                PageListColumnHelper.ExtractAndAddColumns(pageList, initialPage);
            }

            // Create entries in list that have been added by a user
            var tableDbSettings = settings.Where(s => s.Key.StartsWith(pageList.Key)).ToDictionary(k => k.Key, v => v.Value);

            var grouped = tableDbSettings
                .Where(kv => IsValidPageListEntryKey(kv.Key, pageList.Key))
                .GroupBy(k => GetListEntryIndex(k.Key, pageList.Key), (key, value) => new
            {
                Key = key,
                Value = value
            }).OrderBy(k => k.Key).Where(kp => kp.Key >= pageList.PageEntries.Count).ToList();

            foreach (var group in grouped)
            {
                if (pageList.PageStructure is not Page pageStructure)
                {
                    throw new InvalidOperationException("PageStructure is not of type Page.");
                }

                var newKey = pageStructure.Key + PageHelper.SettingsKeyDeLimiter + group.Key;
                var segments = pageStructure.Url.Split(PageHelper.SettingsLabelDeLimiter).SkipLast(1);
                var baseUrl = string.Join(PageHelper.SettingsKeyDeLimiter, segments);

                var labelKey = newKey + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName;
                if (!tableDbSettings.TryGetValue(labelKey, out var pageLabel))
                {
                    var keyNoIndex = pageStructure.Key + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName;
                    pageLabel = pageStructure.Children.OfType<Option>().FirstOrDefault(c => c.Key == keyNoIndex)?.Value
                        ?? GetProviderManagedFallbackEntryLabel(pageList, tableDbSettings, newKey);
                }

                var keyPathWithoutIndex = baseUrl + PageHelper.SettingsKeyDeLimiter + pageLabel;
                var url = baseUrl.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter) + PageHelper.SettingsLabelDeLimiter + pageLabel;

                var item = new Page
                {
                    Key = newKey,
                    Header = pageStructure.Header,
                    Url = url,
                    Label = pageLabel!,
                    Dependency = pageStructure.Dependency,
                    Children = pageStructure.Children.Select(c => c.DeepCopy(newKey, keyPathWithoutIndex)).ToList()
                };

                PopulatePage(item, settings);
                PageListColumnHelper.ExtractAndAddColumns(pageList, item);

                pageList.PageEntries.Add(item);
            }
        }

        private static bool IsValidPageListEntryKey(string key, string parentKey)
        {
            if (key.Length <= parentKey.Length + 1)
            {
                return false;
            }

            var afterParent = key.Substring(parentKey.Length + 1);
            var delimiterIndex = afterParent.IndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.InvariantCulture);

            if (delimiterIndex == -1)
            {
                return false;
            }

            var indexPart = afterParent[..delimiterIndex];
            return int.TryParse(indexPart, CultureInfo.InvariantCulture, out _);
        }

        private static int GetListEntryIndex(string key, string parentKey)
        {
            var afterParent = key.Substring(parentKey.Length + 1); // including ":"
            var index = afterParent[..afterParent.IndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.InvariantCulture)];
            return int.Parse(index, CultureInfo.InvariantCulture);
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

    }
}
