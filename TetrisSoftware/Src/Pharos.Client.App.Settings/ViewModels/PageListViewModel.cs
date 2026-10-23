using System.Globalization;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.ViewModels.Converters;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;

public class PageListViewModel(ISettingsDataService<SettingItemBase> dataService, IConverter converter, IStringLocalizer<Translations> loc) : SettingsViewModelBase(loc)
{
    public List<PagePlaceholder> PageEntries { get; set; } = [];

    public PageViewModel PageStructure { get; set; } = default!;

    public List<string> Headers { get; set; } = [];

    public List<TableRowViewModel> Values { get; set; } = [];

    public IQueryable<TableRowViewModel> VisibleRows => Values.Where(x => x.EntryKey is not null).AsQueryable();

    public List<ChoicesViewModel> DynamicDropdowns { get; set; } = [];

    public bool CopyEnabled { get; set; }

    /// <summary>
    /// When true, this PageList is managed by a provider. Users cannot create, delete, or copy entries.
    /// </summary>
    public bool IsProviderManaged { get; set; }

    public bool CanCopy
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(CanCopy));
        }
    }

    public bool CanEdit
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(CanEdit));
        }
    }

    public bool CanDelete
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(CanDelete));
        }
    }

    public void SelectedItemsChanged(HashSet<TableRowViewModel> obj)
    {
        foreach (var row in Values)
        {
            row.IsSelected = obj.Contains(row);
        }

        CanCopy = CopyEnabled && obj.Count == 1;
        CanEdit = obj.Count == 1;
        var minItems = (Model as PageList)?.MinItems ?? 0;
        CanDelete = obj.Count > 0 && Values.Count > minItems;
    }

    public async Task<PageViewModel> AddNewRow()
    {
        Values.ForEach(v => v.IsSelected = false);
        var nextIdx = PageEntries.Count > 0 ? PageEntries.Max(e => int.Parse(e.Key.Split(':').Last(), CultureInfo.InvariantCulture)) + 1 : 0;

        var entry = await MakeNewEntry(PageStructure, nextIdx);
        var entryPlaceHolder = new PagePlaceholder { Key = entry.Model?.Key, Label = entry.Label, Dependency = entry.Dependency };

        PageEntries.Add(entryPlaceHolder);
        entry.IsLocal = true;
        return entry;
    }

    public async Task<CopySettingsResult> CopySelectedRow()
    {
        var selectedIndex = Values.FindIndex(r => r.IsSelected);
        if (selectedIndex < 0)
        {
            throw new InvalidOperationException("No row selected for copy.");
        }

        var selectedPlaceholder = PageEntries[selectedIndex];
        if (string.IsNullOrWhiteSpace(selectedPlaceholder.Key))
        {
            throw new InvalidOperationException("Selected entry key is missing.");
        }

        return await dataService.CopySettingsAsync(new CopySettingsCommand
        {
            Key = selectedPlaceholder.Key,
            RootPageLabel = selectedPlaceholder.Key.Split(":").First()
        });
    }

    public async Task<PageViewModel> MakeNewEntry(PageViewModel pageStructure, int idx)
    {
        var rootLabel = pageStructure.Key!.Split(":").First();
        var page = (Page)await dataService.GetPageAsync(rootLabel, pageStructure.Key);
        var pageStructureUrl = string.IsNullOrWhiteSpace(pageStructure.Url) ? page.Url : pageStructure.Url;
        var pagePath = pageStructure.Key;
        VisitPage(page, idx, pagePath);
        page.Label = pageStructure.Label + idx;
        page.Url = BuildEntryUrl(pageStructureUrl, idx);
        var pageViewModel = (PageViewModel)converter.Convert(page);
        return pageViewModel;

    }

    private static string BuildEntryUrl(string pageStructureUrl, int entryIndex)
    {
        var indexStr = entryIndex.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(pageStructureUrl))
        {
            return indexStr;
        }

        var segments = pageStructureUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).SkipLast(1);
        var baseUrl = string.Join("/", segments);
        return string.IsNullOrWhiteSpace(baseUrl) ? indexStr : $"{baseUrl}/{indexStr}";
    }

    private static void VisitChild(SettingsChildItem child, int idx, string pageStructureKey)
    {
        switch (child)
        {
            case var _ when child is Group group:
                VisitGroup(group, idx, pageStructureKey);
                break;

            case var _ when child is PageList pageList:
                VisitPageList(pageList, idx, pageStructureKey);
                break;

            case var _ when child is Option option:
                VisitOption(option, idx, pageStructureKey);
                break;

            case var _ when child is PagePlaceholder pagePlaceholder:
                VisitPagePlaceholder(pagePlaceholder, idx, pageStructureKey);
                break;
        }
    }

    private static void VisitPage(Page page, int idx, string pageStructureKey)
    {
        UpdateKeyAndDependency(page, idx, pageStructureKey);

        foreach (var child in page.Children)
        {
            VisitChild(child, idx, pageStructureKey);
        }
    }

    private static void VisitPagePlaceholder(PagePlaceholder pagePlaceholder, int idx, string pageStructureKey)
    {
        UpdateKeyAndDependency(pagePlaceholder, idx, pageStructureKey);
    }

    private static void VisitOption(Option option, int idx, string pageStructureKey)
    {
        UpdateKeyAndDependency(option, idx, pageStructureKey);
        UpdateNonOverlappingRanges(option, idx, pageStructureKey);
    }

    private static void VisitGroup(Group group, int idx, string pageStructureKey)
    {
        UpdateKeyAndDependency(group, idx, pageStructureKey);
        foreach (var child in group.Children)
        {
            VisitChild(child, idx, pageStructureKey);
        }
    }

    private static void VisitPageList(PageList pageList, int idx, string pageStructureKey)
    {
        foreach (var pagePlaceholder in pageList.PageEntries)
        {
            pagePlaceholder.Key = AddKey(pagePlaceholder.Key, idx, pageStructureKey);
        }
    }

    private static void UpdateKeyAndDependency(SettingItemBase item, int idx, string pageStructureKey)
    {
        item.Key = AddKey(item.Key, idx, pageStructureKey);

        if (item.Dependency != null)
        {
            foreach (var dependency in item.Dependency)
            {
                dependency.MainOptionKey = AddKey(dependency.MainOptionKey, idx, pageStructureKey);
            }
        }
    }

    private static void UpdateNonOverlappingRanges(Option option, int idx, string pageStructureKey)
    {
        if (option.NonOverlappingRanges is null || option.NonOverlappingRanges.Count == 0)
        {
            return;
        }

        option.NonOverlappingRanges = option.NonOverlappingRanges
            .Select(validation => new NonOverlappingRangesValidation
            {
                FirstStartKey = AddKey(validation.FirstStartKey, idx, pageStructureKey),
                FirstLengthKey = AddKey(validation.FirstLengthKey, idx, pageStructureKey),
                SecondStartKey = AddKey(validation.SecondStartKey, idx, pageStructureKey),
                SecondLengthKey = AddKey(validation.SecondLengthKey, idx, pageStructureKey),
                EnabledWhen = (validation.EnabledWhen ?? [])
                    .Select(dependency => new Dependency
                    {
                        MainOptionKey = AddKey(dependency.MainOptionKey, idx, pageStructureKey),
                        MainOptionValue = dependency.MainOptionValue,
                        Type = dependency.Type
                    })
                    .ToList(),
                Error = validation.Error
            })
            .ToList();
    }

    private static string AddKey(string itemKey, int idx, string pageStructureKey)
    {
        var pagePathSegments = pageStructureKey.Split(':').ToList();
        var segments = itemKey.Split(':').ToList();
        var additionalSegments = segments.Skip(pagePathSegments.Count).ToList();

        return additionalSegments.Count > 0 ? $"{pageStructureKey}:{idx}:{string.Join(":", additionalSegments)}" : $"{itemKey}:{idx}";
    }

    public override bool Save()
    {
        IsModified = false;

        return true;
    }
}
