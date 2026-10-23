using System.Globalization;
using Pharos.Server.Settings.Application.Commands.CopySettings;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Application.Services;

internal sealed class SettingsCopy(IPageTreeNavigator pageTreeNavigator) : ISettingsCopy
{
    public string? FindPageListKeyForSourcePage(Dictionary<string, Page> rootPages, string entryKey)
    {
        var pageList = pageTreeNavigator.FindPageListContainingEntry(rootPages, entryKey);
        return pageList?.Key;
    }

    public SettingsCopyResult CreateCopy(Dictionary<string, Page> populatedPages,
        string sourcePageKey, Func<string, bool> settingKeyExistsPredicate, int actualItemCount)
    {
        // 1. Source page
        if (!populatedPages.TryGetValue(sourcePageKey, out var sourcePage))
        {
            return new SettingsCopyResult(false, CopySettingsErrorCode.SourcePageNotFound, null, Array.Empty<Setting>());
        }

        // 2. Find PageList
        var pageList = pageTreeNavigator.FindPageListContainingEntry(populatedPages, sourcePage.Key);
        if (pageList is null || !pageList.HasCopyFunction)
        {
            return new SettingsCopyResult(false, CopySettingsErrorCode.CopyDisabled, null, Array.Empty<Setting>());
        }

        // 2.1. Check max items limit
        if (pageList.MaxItems.HasValue && actualItemCount >= pageList.MaxItems.Value)
        {
            return new SettingsCopyResult(false, CopySettingsErrorCode.MaxItemsReached, null, Array.Empty<Setting>(), IsWarning: true);
        }

        // 3. Index + new key
        var existingIndices = GetExistingIndices(pageList);
        var nextIndex = GetNextSequentialIndex(existingIndices);
        var newPageKey = ComputeNewPageKey(pageList, nextIndex);

        // 4. Deep copy base page
        var (newPage, nameOption) = CreateCopiedPage(sourcePage, newPageKey);

        // 5. Apply copy attribute overrides
        ApplyCopyAttributeValues(newPage);

        // 6. Adjust name uniqueness + labels
        if (nameOption is not null)
        {
            var proposed = nameOption.Value ?? nameOption.Label;
            if (!string.IsNullOrWhiteSpace(proposed))
            {
                var adjustedName = AdjustNameForCopy(pageList, proposed!);
                if (adjustedName.Length > nameOption.MaxLength)
                {
                    return new SettingsCopyResult(false, CopySettingsErrorCode.NameTooLong, null, Array.Empty<Setting>(), IsWarning: true);
                }
                nameOption.Value = adjustedName;
            }
            UpdateNewPageLabelAndRootPath(sourcePage, newPage, nameOption);
        }

        // 7. Rebase nested LabelFromRoot paths
        RebaseDescendantLabelPaths(newPage, newPage.Url);

        // 8. Mutate graph (add new page to list & lookup)
        pageList.PageEntries.Add(newPage);
        populatedPages[newPage.Key] = newPage;

        // 9. Extract settings to persist (non-null, non-default, non-existing)
        var newOptions = PageHelper.FindOptions(newPage).Values;
        var settingsToPersist = newOptions
            .Where(opt => opt.Value is not null)
            .Where(opt => !Equals(opt.Value, opt.DefaultValue))
            .Where(opt => !settingKeyExistsPredicate(opt.Key))
            .Select(SettingValueStorage.CreateSetting)
            .ToList();

        var success = true; // We succeeded in creating the copy regardless of whether there are settings to save.
        return new SettingsCopyResult(success, null, newPage, settingsToPersist);
    }

    private static HashSet<int> GetExistingIndices(PageList pageList)
    {
        var baseKey = pageList.Key;
        return pageList.PageEntries
            .Select(e => TryGetIndex(e.Key, baseKey))
            .Where(i => i >= 0)
            .ToHashSet();
    }

    private static int TryGetIndex(string key, string baseListKey)
    {
        var prefix = baseListKey + PageHelper.SettingsKeyDeLimiter;
        if (!key.StartsWith(prefix, StringComparison.InvariantCulture))
        {
            return -1;
        }

        var remainder = key[prefix.Length..];
        var firstDelimiter = remainder.IndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.InvariantCulture);
        var indexSegment = firstDelimiter >= 0 ? remainder[..firstDelimiter] : remainder;
        return int.TryParse(indexSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) ? idx : -1;
    }

    private static int GetNextSequentialIndex(HashSet<int> existing)
    {
        if (existing.Count == 0)
        {
            return 0;
        }
        var next = existing.Max() + 1;
        if (next < 0)
        {
            throw new InvalidOperationException("Index overflow while generating next page index.");
        }
        return next;
    }

    private static string ComputeNewPageKey(PageList pageList, int nextIndex) =>
        $"{pageList.Key}{PageHelper.SettingsKeyDeLimiter}{nextIndex}";

    private static (Page newPage, Option? nameOption) CreateCopiedPage(Page sourcePage, string newPageKey)
    {
        var baseLabelSegments = sourcePage.Url
            .Split(PageHelper.SettingsLabelDeLimiter, StringSplitOptions.RemoveEmptyEntries)
            .SkipLast(1);
        var baseUrl = string.Join(PageHelper.SettingsKeyDeLimiter, baseLabelSegments);

        var sourceNameOption = sourcePage.Children
            .OfType<Option>()
            .FirstOrDefault(o => o.IsNameColumn);

        var nameValue = sourceNameOption?.Value ?? sourceNameOption?.Label ?? "Copy";
        var keyPathWithoutIndex = $"{baseUrl}{PageHelper.SettingsKeyDeLimiter}{nameValue}";

        var newPage = new Page
        {
            Key = newPageKey,
            Header = sourcePage.Header,
            Label = sourcePage.Label,
            Url = sourcePage.Url,
            Dependency = sourcePage.Dependency,
            Children = sourcePage.Children.Select(c => c.DeepCopy(newPageKey, keyPathWithoutIndex)).ToList()
        };

        var newNameOption = newPage.Children
            .OfType<Option>()
            .FirstOrDefault(o => o.IsNameColumn);

        return (newPage, newNameOption);
    }

    private static void ApplyCopyAttributeValues(Page page)
    {
        var allOptions = PageHelper.FindOptions(page);
        foreach (var option in allOptions.Values)
        {
            if (option.ValueAfterCopy is null)
            {
                continue;
            }

            option.Value = option.ValueAfterCopy.ToString();
        }
    }

    private static string AdjustNameForCopy(PageList pageList, string proposedName)
    {
        // Existing labels for uniqueness checks
        var existing = pageList.PageEntries
            .OfType<Page>()
            .Select(p => p.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // If no conflict, keep proposed as-is
        if (!existing.Contains(proposedName))
        {
            return proposedName;
        }

        var baseName = proposedName;
        // Strip only trailing digits from proposed, keep any existing "_COPY" segments
        if (proposedName.EndsWith("_COPY", StringComparison.OrdinalIgnoreCase))
        {
            baseName = StripTrailingDigits(proposedName);
        }

        // First candidate: append another "_COPY"
        var candidate = baseName + "_COPY";
        if (!existing.Contains(candidate))
        {
            return candidate;
        }

        // If that exists, increment numeric suffix starting at 2
        var counter = 2;
        while (existing.Contains(candidate + counter))
        {
            counter++;
        }

        return candidate + counter;
    }

    // Helper: removes only trailing digits (preserves any "_COPY" chain)
    private static string StripTrailingDigits(string name)
    {
        var i = name.Length;
        while (i > 0 && char.IsDigit(name[i - 1]))
        {
            i--;
        }
        return name[..i];
    }

    private static void UpdateNewPageLabelAndRootPath(Page sourcePage, Page newPage, Option? nameOption)
    {
        if (nameOption is null)
        {
            return;
        }

        var baseSegments = sourcePage.Url
            .Split(PageHelper.SettingsLabelDeLimiter, StringSplitOptions.RemoveEmptyEntries)
            .SkipLast(1);
        var basePath = string.Join(PageHelper.SettingsLabelDeLimiter, baseSegments);

        var entryIndex = newPage.Key.Split(PageHelper.SettingsKeyDeLimiter)[^1];
        newPage.Label = nameOption.Value ?? nameOption.Label;
        newPage.Url = $"{basePath}{PageHelper.SettingsLabelDeLimiter}{entryIndex}";
    }

    private static void RebaseDescendantLabelPaths(Page page, string rootLabelFromRoot)
    {
        foreach (var child in page.Children)
        {
            switch (child)
            {
                case Page nestedPage:
                    {
                        var tail = Tail(nestedPage.Url);
                        nestedPage.Url = $"{rootLabelFromRoot}{PageHelper.SettingsLabelDeLimiter}{tail}";
                        RebaseDescendantLabelPaths(nestedPage, nestedPage.Url);
                        break;
                    }
                case Group group:
                    {
                        var gTail = Tail(group.Url);
                        group.Url = $"{rootLabelFromRoot}{PageHelper.SettingsLabelDeLimiter}{gTail}";
                        RebaseGroupLabelPaths(group, group.Url);
                        break;
                    }
                case PageList list:
                    {
                        if (list.PageStructure is Page structure)
                        {
                            structure.Url = $"{rootLabelFromRoot}{PageHelper.SettingsLabelDeLimiter}{structure.Label}";
                            RebaseDescendantLabelPaths(structure, structure.Url);
                        }
                        foreach (var entry in list.PageEntries.OfType<Page>())
                        {
                            var entryIndex = entry.Key.Split(PageHelper.SettingsKeyDeLimiter)[^1];
                            entry.Url = $"{rootLabelFromRoot}{PageHelper.SettingsLabelDeLimiter}{entryIndex}";
                            RebaseDescendantLabelPaths(entry, entry.Url);
                        }
                        break;
                    }
            }
        }
    }

    private static void RebaseGroupLabelPaths(Group group, string parentLabelFromRoot)
    {
        foreach (var child in group.Children)
        {
            switch (child)
            {
                case Page nestedPage:
                    {
                        var tail = Tail(nestedPage.Url);
                        nestedPage.Url = $"{parentLabelFromRoot}{PageHelper.SettingsLabelDeLimiter}{tail}";
                        RebaseDescendantLabelPaths(nestedPage, nestedPage.Url);
                        break;
                    }
                case Group nestedGroup:
                    {
                        var gTail = Tail(nestedGroup.Url);
                        nestedGroup.Url = $"{parentLabelFromRoot}{PageHelper.SettingsLabelDeLimiter}{gTail}";
                        RebaseGroupLabelPaths(nestedGroup, nestedGroup.Url);
                        break;
                    }
            }
        }
    }

    private static string Tail(string labelFromRoot)
    {
        return labelFromRoot.Split(PageHelper.SettingsLabelDeLimiter, StringSplitOptions.RemoveEmptyEntries).Last();
    }
}
