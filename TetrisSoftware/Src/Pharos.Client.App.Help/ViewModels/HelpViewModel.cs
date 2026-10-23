using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Models.HelpContent;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.ChapterNavigation;

namespace Pharos.Client.App.Help.ViewModels;

public class HelpViewModel(IHelpContentService helpDataSource) : LoadingViewModel
{
    private const int _maxNavigationDepth = 3;

    public Dictionary<string, (string rootChapterId, List<string> path)> ChapterStructure { get; private set; } = [];
    public HelpContentManual? HelpContentManual { get; private set; }
    public List<ChapterNode> ChapterNavNodes { get; private set; } = [];

    public bool IsLoaded { get; set; }

    public string GetManualId(string chapterId)
    {
        if (ChapterStructure.TryGetValue(chapterId, out var rootChapter))
        {
            return rootChapter.rootChapterId;
        }

        return chapterId;
    }

    public bool ChapterExists([NotNullWhen(true)] string? chapterId)
    {
        return chapterId is not null && ChapterStructure.ContainsKey(chapterId);
    }

    public string? GetFirstChapterId()
    {
        return HelpContentManual?.Chapters?.FirstOrDefault()?.Id;
    }

    public string? GetClosestMenuChapterId(string? chapterId)
    {
        if (string.IsNullOrWhiteSpace(chapterId) ||
            !ChapterStructure.TryGetValue(chapterId, out var chapterData) ||
            chapterData.path.Count == 0)
        {
            return null;
        }

        var index = Math.Min(_maxNavigationDepth, chapterData.path.Count) - 1;
        return chapterData.path[index];
    }

    protected override async Task DoLoadAsync()
    {
        HelpContentManual = await helpDataSource.GetManual();
        ChapterStructure = await helpDataSource.GetChapterStructure();

        ChapterNavNodes = MapChapterToNavNode(HelpContentManual?.Chapters);
        IsLoaded = true;
    }


    private static List<ChapterNode> MapChapterToNavNode(List<Chapter>? chapters, int navigationLevel = 1)
    {
        if (navigationLevel > _maxNavigationDepth || chapters is null)
        {
            return [];
        }

        return chapters.Select(chapter => new ChapterNode
        {
            Id = chapter.Id.ToString(CultureInfo.InvariantCulture),
            Title = chapter.Title ?? string.Empty,
            Href = $"{PageRoute.Help}/#{chapter.Id}",
            Children = MapChapterToNavNode(chapter.Chapters, navigationLevel + 1)
        }).ToList();
    }
}
