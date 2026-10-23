using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Settings;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Services;

internal class SettingsPagesLoader : ISettingsPagesLoader
{
    private const string _noDisplayNameAvailable = "No display name available";

    private readonly ISettingsDbContext _db;
    private readonly ISettingsStringLocalizer _localizer;
    private readonly IServiceProvider _serviceProvider;
    private Task<Dictionary<string, string?>>? _settingsLoadTask;

    public SettingsPagesLoader(
        ISettingsDbContext db,
        ISettingsStringLocalizer localizer,
        IServiceProvider serviceProvider)
    {
        _db = db;
        _localizer = localizer;
        _serviceProvider = serviceProvider;
    }

    public async Task<Dictionary<string, Page>> LoadAsync(string rootPageLabel, CancellationToken token)
    {
        var rootType = SettingsPagesProvider.GetRootType(_localizer, rootPageLabel);
        IEnumerable<Type> SelectComplex(Assembly a) => a.GetTypes().Where(t => t == rootType);

        var settings = await GetSettingsAsync(token).ConfigureAwait(false);
        var pages = SettingsPagesProvider.GetAllPages(_localizer, SelectComplex);
        return await PopulatePagesAsync(pages, settings, token).ConfigureAwait(false);
    }

    public async Task<Dictionary<string, Page>> LoadAllAsync(CancellationToken token)
    {
        var settings = await GetSettingsAsync(token).ConfigureAwait(false);
        var pages = SettingsPagesProvider.GetAllPages(_localizer);
        return await PopulatePagesAsync(pages, settings, token).ConfigureAwait(false);
    }

    private async Task<Dictionary<string, string?>> GetSettingsAsync(CancellationToken token)
    {
        _settingsLoadTask ??= LoadSettingsAsync(token);

        try
        {
            return await _settingsLoadTask.ConfigureAwait(false);
        }
        catch
        {
            _settingsLoadTask = null;
            throw;
        }
    }

    private async Task<Dictionary<string, string?>> LoadSettingsAsync(CancellationToken token)
    {
        var storedSettings = await _db.Settings
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        return SettingValueStorage.ToSettingsDictionary(
            storedSettings,
            BaseDefaultSettingsProvider.GetDefaultOptions());
    }

    private async Task<Dictionary<string, Page>> PopulatePagesAsync(
        Dictionary<string, Page> pages,
        Dictionary<string, string?> settings,
        CancellationToken token)
    {
        var populated = new Dictionary<string, Page>();

        foreach (var page in pages.Values)
        {
            var populator = PopulatorFactory.GetPopulator(page);
            populator?.Populate(page, settings, ref populated);
        }

        await EnsureProviderManagedEntriesAsync(pages, settings, populated, token);

        return populated;
    }

    public async Task<Page?> TryGetAsync(string pageKey, string rootPageLabel, CancellationToken token)
    {
        var dict = await LoadAsync(rootPageLabel, token).ConfigureAwait(false);
        return dict.TryGetValue(pageKey, out var page) ? page : null;
    }

    private async Task EnsureProviderManagedEntriesAsync(
        Dictionary<string, Page> pages,
        Dictionary<string, string?> settings,
        Dictionary<string, Page> populated,
        CancellationToken token)
    {
        var providerManagedPageLists = CollectProviderManagedPageLists(pages.Values);
        if (providerManagedPageLists.Count == 0)
        {
            return;
        }

        foreach (var pageList in providerManagedPageLists)
        {
            if (string.IsNullOrWhiteSpace(pageList.EntryProviderKey) || pageList.PageStructure is not Page pageStructure)
            {
                continue;
            }

            var providerKeyPropertyName = GetProviderKeyPropertyName(pageList);
            var provider = _serviceProvider.GetRequiredKeyedService<IPageListEntryProvider>(pageList.EntryProviderKey);
            var entries = await provider.GetEntriesAsync(token);
            var activeKeys = entries.Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var nextIndex = GetNextIndex(pageList, settings);

            // Remove entries whose provider key is no longer provided.
            var staleEntries = pageList.PageEntries
                .OfType<Page>()
                .Where(e => GetEntryKey(e, providerKeyPropertyName) is not { } key || !activeKeys.Contains(key))
                .Cast<ChildPageItem>()
                .ToList();
            foreach (var stale in staleEntries)
            {
                pageList.PageEntries.Remove(stale);
                RemovePopulatedPageTree(populated, stale.Key);
            }

            var existingByEntryKey = pageList.PageEntries
                .OfType<Page>()
                .Select(page => new { Page = page, EntryKey = GetEntryKey(page, providerKeyPropertyName) })
                .Where(x => !string.IsNullOrWhiteSpace(x.EntryKey))
                .ToDictionary(x => x.EntryKey!, x => x.Page, StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                if (existingByEntryKey.TryGetValue(entry.Key, out var existing))
                {
                    ApplyProviderManagedEntryMetadata(existing, pageStructure, entry, providerKeyPropertyName);
                    continue;
                }

                var newKey = pageStructure.Key + PageHelper.SettingsKeyDeLimiter + nextIndex;
                var displayName = GetDisplayName(entry);
                var (entryPath, url) = BuildEntryPaths(pageStructure.Url, nextIndex);

                var item = new Page
                {
                    Key = newKey,
                    Header = pageStructure.Header,
                    Url = url,
                    Label = displayName,
                    Dependency = pageStructure.Dependency,
                    Children = pageStructure.Children.Select(c => c.DeepCopy(newKey, entryPath)).ToList()
                };

                ApplyProviderManagedEntryMetadata(item, pageStructure, entry, providerKeyPropertyName);

                // Populate with any existing DB values
                var populator = PopulatorFactory.GetPopulator(item);
                populator?.Populate(item, settings, ref populated);

                populated.TryAdd(item.Key, item);
                pageList.PageEntries.Add(item);

                nextIndex++;
            }

            PageListColumnHelper.Rebuild(pageList);
        }
    }

    private static List<PageList> CollectProviderManagedPageLists(IEnumerable<Page> pages)
    {
        var result = new List<PageList>();
        foreach (var page in pages)
        {
            CollectFromChildren(page.Children, result);
        }
        return result;
    }

    private static void CollectFromChildren(IEnumerable<SettingsChildItem> children, List<PageList> result)
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case PageList { IsProviderManaged: true } pageList:
                    result.Add(pageList);
                    break;
                case PageList pageList when pageList.PageStructure is Page structurePage:
                    CollectFromChildren(structurePage.Children, result);
                    break;
                case Page page:
                    CollectFromChildren(page.Children, result);
                    break;
                case Group group:
                    CollectFromChildren(group.Children, result);
                    break;
            }
        }
    }

    private static (string entryPath, string url) BuildEntryPaths(string pageStructureUrl, int entryIndex)
    {
        var indexStr = entryIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var segments = pageStructureUrl.Split(PageHelper.SettingsLabelDeLimiter).SkipLast(1);
        var baseUrl = string.Join(PageHelper.SettingsKeyDeLimiter, segments);
        var entryPath = baseUrl + PageHelper.SettingsKeyDeLimiter + indexStr;
        var url = baseUrl.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter) + PageHelper.SettingsLabelDeLimiter + indexStr;
        return (entryPath, url);
    }

    private static void ApplyProviderManagedEntryMetadata(Page page, Page pageStructure, PageListEntry entry, string providerKeyPropertyName)
    {
        var providerDisplayName = GetDisplayName(entry);
        var nameOption = FindEntryOption(page, PageList.RequiredTablePropertyName);
        var displayName = GetEntryDisplayName(nameOption, providerDisplayName);
        var entryIndex = int.Parse(page.Key.Split(PageHelper.SettingsKeyDeLimiter)[^1], System.Globalization.CultureInfo.InvariantCulture);
        var (entryPath, url) = BuildEntryPaths(pageStructure.Url, entryIndex);

        page.Label = displayName;
        page.Url = url;

        var entryKeyOption = FindEntryOption(page, providerKeyPropertyName)
            ?? throw new InvalidOperationException($"Provider-managed PageList entry at key '{page.Key}' is missing key option '{providerKeyPropertyName}'.");

        entryKeyOption.Value = entry.Key;
        entryKeyOption.IsHidden = true;

        UpdateDescendantUrls(page, entryPath);
    }

    private static string GetEntryDisplayName(Option? nameOption, string providerDisplayName)
    {
        if (nameOption == null)
        {
            return providerDisplayName;
        }

        if (nameOption.IsReadOnly || string.IsNullOrWhiteSpace(nameOption.Value))
        {
            nameOption.Value = providerDisplayName;
            return providerDisplayName;
        }

        return nameOption.Value;
    }

    private static string GetProviderKeyPropertyName(PageList pageList)
    {
        return pageList.ProviderKeyPropertyName
            ?? throw new InvalidOperationException($"Provider-managed PageList at key '{pageList.Key}' is missing its provider key property name.");
    }

    private static string GetDisplayName(PageListEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.DisplayName) ? _noDisplayNameAvailable : entry.DisplayName;
    }

    private static Option? FindEntryOption(Page page, string propertyName)
    {
        var optionKey = page.Key + PageHelper.SettingsKeyDeLimiter + propertyName;
        return page.Children.OfType<Option>().FirstOrDefault(c => c.Key == optionKey);
    }

    private static string? GetEntryKey(Page page, string providerKeyPropertyName)
    {
        return FindEntryOption(page, providerKeyPropertyName)?.Value;
    }

    private static int GetNextIndex(PageList pageList, Dictionary<string, string?> settings)
    {
        var maxEntryIndex = pageList.PageEntries
            .Select(e => TryGetIndex(e.Key, pageList.Key));

        var maxSettingIndex = settings.Keys
            .Where(k => k.StartsWith(pageList.Key + PageHelper.SettingsKeyDeLimiter, StringComparison.Ordinal))
            .Select(k => TryGetIndex(k, pageList.Key));

        return maxEntryIndex
            .Concat(maxSettingIndex)
            .Where(i => i.HasValue)
            .Select(i => i!.Value)
            .DefaultIfEmpty(-1)
            .Max() + 1;
    }

    private static void RemovePopulatedPageTree(Dictionary<string, Page> populated, string pageKey)
    {
        var keysToRemove = populated.Keys
            .Where(key => key.Equals(pageKey, StringComparison.Ordinal)
                || key.StartsWith(pageKey + PageHelper.SettingsKeyDeLimiter, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            populated.Remove(key);
        }
    }

    private static void UpdateDescendantUrls(ISettingsParentItem parent, string parentPath)
    {
        foreach (var child in parent.Children)
        {
            switch (child)
            {
                case Page page:
                {
                    var childLabelPath = parentPath + PageHelper.SettingsKeyDeLimiter + page.Label;
                    page.Url = childLabelPath.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter);
                    UpdateDescendantUrls(page, childLabelPath);
                    break;
                }
                case Group group:
                {
                    var childLabelPath = parentPath + PageHelper.SettingsKeyDeLimiter + group.Label;
                    group.Url = childLabelPath.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter);
                    UpdateDescendantUrls(group, childLabelPath);
                    break;
                }
                case PageList pageList when pageList.PageStructure is Page pageStructure:
                {
                    var childLabelPath = parentPath + PageHelper.SettingsKeyDeLimiter + pageList.Label;
                    var childUrl = childLabelPath.Replace(PageHelper.SettingsKeyDeLimiter, PageHelper.SettingsLabelDeLimiter);
                    pageList.Url = childUrl;
                    pageStructure.Url = childUrl;
                    UpdateDescendantUrls(pageStructure, childLabelPath);
                    break;
                }
            }
        }
    }

    private static int? TryGetIndex(string key, string parentKey)
    {
        var afterParentStart = parentKey.Length + 1;
        if (key.Length <= afterParentStart)
        {
            return null;
        }

        var afterParent = key[afterParentStart..];
        var delimiterIndex = afterParent.IndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.InvariantCulture);
        var indexStr = delimiterIndex == -1 ? afterParent : afterParent[..delimiterIndex];
        return int.TryParse(indexStr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var index)
            ? index
            : null;
    }
}
