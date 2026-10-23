using System.Text.RegularExpressions;

namespace Pharos.Client.App.Settings.Models;

public sealed partial class SettingsDataService : ISettingsDataService<SettingItemBase>
{
    private const string _idPattern = @":[0-9]+$";

    private readonly HttpClient _httpClient;
    private Task<Dictionary<string, string>>? _pageRouteMapTask;

    public SettingsDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Each one of these pages contains the items relative to a specific key.
    /// This can be used to verify whether a value with the Unique attribute is
    /// already defined elsewhere.
    /// </summary>
    public IEnumerable<Page> KeyItemsPages { get; private set; } = [];

    public async Task<SettingItemBase> GetPageAsync(string rootPageLabel, string key)
    {
        var client = new SettingsClient(_httpClient);
        var result = await client.GetPageAsync(key, rootPageLabel);

        KeyItemsPages = await RetrieveSameSettingsPages(rootPageLabel, key, client);

        return result;
    }

    private Task<Dictionary<string, string>> GetCachedPageRouteMapAsync() =>
        GetPageRouteMapAsync(forceRefresh: false);

    public Task<Dictionary<string, string>> RefreshPageRouteMapAsync(CancellationToken cancellationToken = default) =>
        GetPageRouteMapAsync(forceRefresh: true, cancellationToken);

    private async Task<Dictionary<string, string>> GetPageRouteMapAsync(
    bool forceRefresh,
    CancellationToken cancellationToken = default)
    {
        Task<Dictionary<string, string>> routeMapLoadTask;

        if (forceRefresh || _pageRouteMapTask is null)
        {
            routeMapLoadTask = LoadPageRouteMapAsync(cancellationToken);
            _pageRouteMapTask = routeMapLoadTask;
        }
        else
        {
            routeMapLoadTask = _pageRouteMapTask;
        }

        try
        {
            return await routeMapLoadTask;
        }
        catch
        {
            if (ReferenceEquals(_pageRouteMapTask, routeMapLoadTask))
            {
                _pageRouteMapTask = null;
            }

            throw;
        }
    }

    private async Task<Dictionary<string, string>> LoadPageRouteMapAsync(
        CancellationToken cancellationToken = default)
    {
        var client = new SettingsClient(_httpClient);

        var result = await client.GetPageKeyMapAsync(
            cancellationToken);

        return result.ToDictionary();
    }

    public async Task<Dictionary<string, string>> GetMenuItemsAsync()
    {
        var client = new SettingsClient(_httpClient);
        var result = await client.GetMenuItemsAsync();

        return result.ToDictionary();
    }

    public async Task UpdateAsync(SettingItemBase value)
    {
        await UpdateSettingAsync(value, false);
    }

    public Task<SettingsUpdateResult> UpdateSettingAsync(SettingItemBase value, bool isPageListUpdate)
    {
        var client = new SettingsClient(_httpClient);
        switch (value)
        {
            case Page page:
                {
                    var command = page.ToUpdateSettingCommand();
                    command.IsPageListUpdate = isPageListUpdate;
                    return client.UpdateSettingAsync(page.Key, command);
                }
            default:
                throw new InvalidOperationException("Unknown SettingItemBase type.");
        }
    }

    public async Task DeleteSettingsAsync(string[] keys)
    {
        var client = new SettingsClient(_httpClient);
        await client.DeleteSettingsAsync(keys);
    }

    public async Task<DeleteResponse> DeletePagesAsync(string[] pageListEntryKeys)
    {
        var client = new SettingsClient(_httpClient);
        return await client.DeletePagesAsync(pageListEntryKeys);
    }

    public async Task<string> ExportSettingsAsync()
    {
        var client = new SettingsClient(_httpClient);
        return await client.GetSettingsExportAsync();
    }

    public async Task<SettingsImportResult> ImportSettingsAsync(ImportSettingsCommand importSettingsCommand)
    {
        var client = new SettingsClient(_httpClient);
        return await client.ImportSettingsAsync(importSettingsCommand);
    }

    public async Task<CopySettingsResult> CopySettingsAsync(CopySettingsCommand copySettingsCommand)
    {
        var client = new SettingsClient(_httpClient);
        return await client.CopySettingsAsync(copySettingsCommand);
    }

    public async Task<CheckPageListMaxItemsResult> CheckPageListMaxItemsAsync(string pageListKey, string rootPageLabel)
    {
        var client = new SettingsClient(_httpClient);
        return await client.CheckPageListMaxItemsAsync(pageListKey, rootPageLabel);
    }

    private async Task<IEnumerable<Page>> RetrieveSameSettingsPages(
        string rootPageLabel,
        string key,
        SettingsClient client)
    {
        try
        {
            return await RetrieveSameSettingsPages(
                rootPageLabel,
                key,
                client,
                forceRefreshPageRouteMap: false);
        }
        catch (Exception ex) when (MissingSettingsPageExceptionMatcher.IsMatch(ex))
        {
            return await RetrieveSameSettingsPages(
                rootPageLabel,
                key,
                client,
                forceRefreshPageRouteMap: true);
        }
    }

    private async Task<IEnumerable<Page>> RetrieveSameSettingsPages(
        string rootPageLabel,
        string key,
        SettingsClient client,
        bool forceRefreshPageRouteMap)
    {
        if (rootPageLabel == key)
        {
            return [];
        }

        var pages = new List<Page>();

        var idRegex = GetIdRegex();
        var idMatch = idRegex.Match(key);

        // if the match is successful this is an update of an item, otherwise a creation
        var prefixSegments = idMatch.Success ? key[..idMatch.Index] : key;

        var itemPattern = $@"^{prefixSegments}{_idPattern}";
        var itemRegex = new Regex(itemPattern);

        var pageRouteMap = forceRefreshPageRouteMap
            ? await RefreshPageRouteMapAsync()
            : await GetCachedPageRouteMapAsync();
        var items = pageRouteMap.Where(p => itemRegex.IsMatch(p.Value))
            .Select(x => x.Value).Distinct().ToList();

        foreach (var item in items)
        {
            var page = await client.GetPageAsync(item, rootPageLabel);
            pages.Add(page);
        }

        return pages;
    }

    [GeneratedRegex(_idPattern)]
    private static partial Regex GetIdRegex();

    #region Not used
    public Task<DataSourceResult<SettingItemBase>> GetAsync(DataSourceRequest request)
    {
        throw new NotImplementedException();
    }
    public Task<int> CreateAsync(SettingItemBase value)
    {
        throw new NotImplementedException();
    }

    public Task<SettingItemBase> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public Task<DeleteResponse> DeleteAsync(int[] entityId)
    {
        throw new NotImplementedException();
    }
    #endregion
}