using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

/// <summary>
/// Helper class to retrieve and export all the settings, including default values not present in the DB.
/// </summary>
internal static class SettingsExporter
{
    /// <summary>
    /// Merges default settings with settings from the DB. Initialized page list entries are excluded from export.
    /// </summary>
    /// <returns></returns>
    internal static Dictionary<string, string> MergeSettingsFromDBWithDefaultSettings(IDictionary<string, (string?, bool)> defaultSettings, Dictionary<string, string?> dbSettings)
    {

        //find page entries from db
        var dbNumericSegments = dbSettings.Keys
                .SelectMany(key => key.Split(PageHelper.SettingsKeyDeLimiter)
                    .Select((segment, index) => (Key: key, Value: segment, Index: index)))
                .Where(x => int.TryParse(x.Value, out _))
                .ToList();


        // gather pagestructure options
        var pageStructures = defaultSettings
            .Where(kvp => kvp.Value.Item2 && kvp.Value.Item1 is not null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item1!);

        var defaultSettingsNoPageStructures = defaultSettings
            .Where(kvp => !kvp.Value.Item2 && kvp.Value.Item1 is not null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item1!);

        var numericPageStructures = pageStructures.Keys
            .SelectMany(key => key.Split(PageHelper.SettingsKeyDeLimiter)
                .Select((segment, index) => (Key: key, Value: segment, Index: index)))
            .Where(x => int.TryParse(x.Value, out _))
            .ToList();

        // We don't want to actually do anything with page structures that are not in the DB we just need their prefixes
        var distinctPageStructurePrefixes = ProcessNumericSegments(numericPageStructures, false);

        var distinctByPrefix = ProcessNumericSegments(dbNumericSegments, true);


        var existingPrefixes = distinctByPrefix.Select(p => p.Prefix).ToHashSet(StringComparer.Ordinal);

        var ordered = distinctByPrefix
            .Concat(distinctPageStructurePrefixes.Where(p => !existingPrefixes.Contains(p.Prefix)))
            .OrderByDescending(p => p.Prefix.Length)
            .ToList();

        // Grouped structure from most inside pagelist to root pagelist
        var groupedPrefixes = ordered
            .Select(p => p.Prefix)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(p => p, _ => new List<KeyValuePair<string, string>>(),
                            StringComparer.Ordinal);

        // add page structures to groupedPrefixes
        foreach (var kv in pageStructures)
        {
            var hit = ordered.FirstOrDefault(p => HasSegmentPrefix(kv.Key, p.Prefix));
            // !HasIndexInKey removes initialized pagelistentries from export
            if (hit != null && !HasIndexInKey(kv.Key))
            {
                groupedPrefixes[hit.Prefix].Add(kv);
            }

        }

        var pageListEntries = new Dictionary<string, string>();

        foreach (var pageEntry in ordered)
        {
            var matched = groupedPrefixes[pageEntry.Prefix];
            var pairs = pageEntry.Indices.Zip(pageEntry.Values, (i, v) => (i, v));

            // Remove structure of pagelist inside pagelist in case no entries in DB
            if (pageEntry.Indices.Count == 0 || pageEntry.Values.Count == 0)
            {
                continue;
            }

            var newDict = matched
            .Select((kvp, i) =>
            {
                var segments = kvp.Key.Split(PageHelper.SettingsKeyDeLimiter).ToList();

                foreach (var (idx, val) in pairs)
                {
                    // if initialized entry, they have the key already
                    if (!int.TryParse(segments[idx], out _))
                    {
                        segments.Insert(idx, val);
                    }
                }
                var newKey = string.Join(PageHelper.SettingsKeyDeLimiter, segments);
                return new KeyValuePair<string, string?>(newKey, kvp.Value);
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Add correct pagelist entries with keys
            foreach (var kvp in newDict)
            {
                pageListEntries[kvp.Key] = kvp.Value ?? string.Empty;
            }
        }

        var mergedSettings = defaultSettingsNoPageStructures;

        // add correct PageList Entries
        foreach (var kvp in pageListEntries)
        {
            mergedSettings[kvp.Key] = kvp.Value ?? string.Empty;
        }

        // add values from DB to the options
        foreach (var kvp in dbSettings)
        {
            mergedSettings[kvp.Key] = kvp.Value ?? string.Empty;
        }

        return mergedSettings;
    }

    internal sealed record PageEntry(
        string Key,
        List<string> Values,
        List<int> Indices,
        string Prefix
    );

    static string ComputePrefix(string key, int maxIndex)
    {
        if (maxIndex <= 0)
        {
            return string.Empty;
        };

        // Split once, take up to maxIndex, drop purely numeric segments, then join back.
        var segments = key.Split(PageHelper.SettingsKeyDeLimiter);
        return string.Join(PageHelper.SettingsKeyDeLimiter,
            segments
                .Take(maxIndex)
                .Where(s => !int.TryParse(s, out _)));
    }

    static bool HasSegmentPrefix(string key, string prefix)
    {
        return key.Equals(prefix, StringComparison.Ordinal)
            || key.StartsWith(prefix + PageHelper.SettingsKeyDeLimiter, StringComparison.Ordinal);
    }

    static bool HasIndexInKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var segments = key.Split(PageHelper.SettingsKeyDeLimiter, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(s => int.TryParse(s, out _));
    }

    static List<PageEntry> ProcessNumericSegments(List<(string Key, string Value, int Index)> numericSegments, bool extractValues)
        {
            return numericSegments
                .GroupBy(s => s.Key)
                .Select(g =>
                {
                    var indices = g.Select(x => x.Index)
                               .Distinct()
                               .OrderBy(i => i)
                               .ToList();
                
                    var valuesByIndex = g.GroupBy(x => x.Index)
                     .OrderBy(grp => grp.Key)
                     .Select(grp => grp.First().Value)
                     .ToList();

                    // remove purely numeric segments
                    var prefix = ComputePrefix(g.Key, indices.Max());

                    return new PageEntry(
                        Key: g.Key,
                        Values: extractValues ? valuesByIndex : new List<string>(),
                        Indices: extractValues ? indices : new List<int>(),
                        Prefix: prefix
                    );
                })
                .DistinctBy(e => (e.Prefix, string.Join("\u001F", e.Indices), string.Join("\u001F", e.Values)))
                .ToList();
        }

}
