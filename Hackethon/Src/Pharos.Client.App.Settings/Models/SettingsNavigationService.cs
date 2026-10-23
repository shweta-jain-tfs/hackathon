
namespace Pharos.Client.App.Settings.Models;
public class SettingsNavigationService()
{
    // Mapping from url to page key
    private Dictionary<string, PageKey> _urlKeyMap = new();

    public PageKey GetPageKey(string url)
    {
        if (!TryGetPageKey(url, out var value))
        {
            throw new KeyNotFoundException($"No page with '{url}' was found.");
        }

        return value;
    }

    public bool TryGetPageKey(string url, out PageKey pageKey)
    {
        return _urlKeyMap.TryGetValue(url, out pageKey!);
    }

    public void AddPageKey(string url, string key, bool isLocal = false, SettingsViewModelBase? localPage = null)
    {
        var replacedUrl = url.Replace(":", "/");
        var newEntry = new PageKey { Key = key, IsLocal = isLocal, LocalPage = localPage };
        if (_urlKeyMap.TryGetValue(replacedUrl, out var existing))
        {
            if (existing.IsLocal)
            {
                _urlKeyMap[replacedUrl] = newEntry;
                return;
            }
            throw new ArgumentException($"An entry with the key '{replacedUrl}' already exists.", nameof(url));
        }
        _urlKeyMap[replacedUrl] = newEntry;
    }

    public void SetPageRouteMap(Dictionary<string, string> newPageRouteMap)
    {
        var localEntries = _urlKeyMap.Where(x => x.Value.IsLocal && x.Value.LocalPage is { IsLocal: true }).ToList();

        _urlKeyMap = newPageRouteMap.ToDictionary(u => u.Key, u => new PageKey
        {
            Key = u.Value,
            IsLocal = false
        });

        foreach (var entry in localEntries)
        {
            _urlKeyMap.TryAdd(entry.Key, entry.Value);
        }
    }

    public string GetPathFromKey(string key, string? preferredPath = null)
    {
        if (TryGetPathFromKey(key, preferredPath, out var path))
        {
            return path;
        }

        throw new KeyNotFoundException($"No path with key '{key}' was found.");
    }

    public bool TryGetPathFromKey(string key, string? preferredPath, out string path)
    {
        var matchingPaths = _urlKeyMap
            .Where(x => x.Value.Key == key)
            .Select(x => x.Key)
            .OrderBy(GetSegmentCount)
            .ThenBy(x => x.Length)
            .ToList();

        if (matchingPaths.Count == 0)
        {
            path = string.Empty;
            return false;
        }

        var shortestSegmentCount = GetSegmentCount(matchingPaths.First());
        if (preferredPath != null
            && matchingPaths.Contains(preferredPath)
            && GetSegmentCount(preferredPath) == shortestSegmentCount)
        {
            path = preferredPath;
            return true;
        }

        path = matchingPaths.First();
        return true;
    }

    private static int GetSegmentCount(string path)
    {
        return path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public class PageKey
    {
        public string? Key { get; set; }

        public bool IsLocal { get; set; }

        /// <summary>
        /// This is the view model for a new entry in a PageList, since it does not exist on server before first save.
        /// </summary>
        public SettingsViewModelBase? LocalPage { get; set; }
    }
}
