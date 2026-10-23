using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Pharos.Client.App.Help.Utilities;
using Pharos.Client.App.Help.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models.HelpContent;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Common.Web.Extensions;

namespace Pharos.Client.App.Help.Views;

public partial class HelpPage
{
    [Inject] NavigationManager Navigation { get; set; } = null!;

    [Inject] public HelpViewModel ViewModel { get; set; } = null!;

    [Inject] private IExceptionHandlerService ExceptionHandler { get; set; } = null!;
    [Inject] private IToasterService ToasterService { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IServiceProvider Services { get; set; } = null!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = null!;


    private string? _selectedManualId;
    private string? _selectedChapterId;
    private string? _menuChapterId;
    private bool _failedToLoad;
    private IReadOnlyDictionary<HelpPlaceholderType, string> _placeholderTemplates = new Dictionary<HelpPlaceholderType, string>();

    private IJSObjectReference? _scrollScript;
    private IJSObjectReference? _pageScript;
    private ElementReference _richTextWrapper;
    private DotNetObjectReference<HelpPage>? _dotNetRef;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await using (var htmlRenderer = new HtmlRenderer(Services, LoggerFactory))
            {
                _placeholderTemplates = await HelpContentRenderer.CreateTemplatesAsync(htmlRenderer);
            }

            await ViewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            _failedToLoad = true;
            ExceptionHandler.ShowExceptionMessage(ex, ExceptionDisplayMode.ModalKeepOnNavigation);
        }

        var chapterId = GetChapterIdFromCurrentUrl();
        SetChapterAndScrollValues(chapterId);
    }

    private void SetChapterAndScrollValues(string? chapterId)
    {
        if (ViewModel.ChapterExists(chapterId))
        {
            _selectedChapterId = chapterId;
            _menuChapterId = ViewModel.GetClosestMenuChapterId(chapterId);
            _selectedManualId = ViewModel.GetManualId(chapterId);
            _needsScroll = _selectedManualId != _selectedChapterId;
        }
        else if (!string.IsNullOrWhiteSpace(chapterId))
        {
            ToasterService.ShowToast(ToastType.Information, Loc["HelpPage_ChapterNotFound"]);
        }
    }

    private bool _needsScroll;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _scrollScript = await ImportCommonUtilityJSAsync("scroll.js");
            _pageScript = await ImportJSAsync();
            _dotNetRef ??= DotNetObjectReference.Create(this);
            if (_pageScript is not null)
            {
                await _pageScript.InvokeVoidAsync("startListeningToLinkClicks", _richTextWrapper, _dotNetRef);
            }
        }

        if (_needsScroll && ViewModel.IsLoaded && _selectedChapterId is not null)
        {
            // scroll to chapter after data has loaded
            if (_scrollScript is not null)
            {
                await _scrollScript.InvokeVoidAsync("scrollToElement", _selectedChapterId, "start");
            }

            _needsScroll = false;
        }
    }

    [JSInvokable]
    public Task OnLinkClicked(string? href)
    {
        if (href?.StartsWith($"/{PageRoute.Help}/#", StringComparison.Ordinal) != true)
        {
            return Task.CompletedTask;
        }

        var prevMenuChapterId = _menuChapterId;

        var uri = Navigation.ToAbsoluteUri(href);
        var chapterId = GetChapterIdFromUri(uri);
        SetChapterAndScrollValues(chapterId);

        return prevMenuChapterId != _menuChapterId
            ? InvokeAsync(StateHasChanged) // refresh menu selection and/or switch manual
            : Task.CompletedTask;
    }

    private string? GetChapterIdFromCurrentUrl()
    {
        var uri = new Uri(Navigation.Uri);
        return GetChapterIdFromUri(uri);
    }

    private static string? GetChapterIdFromUri(Uri uri)
    {
        // Get the hash part (e.g., "#123")
        var hash = uri.Fragment;

        if (!string.IsNullOrEmpty(hash) && hash.StartsWith('#'))
        {
            // Try to parse the chapterId from the hash
            var chapterIdFromHash = hash[1..];
            return chapterIdFromHash;
        }

        return null;
    }

    private RenderFragment RenderChapter(IEnumerable<Chapter> chapters, int headerLevel = 1)
    {
        return builder =>
        {
            var seq = 0;

            foreach (var chapter in chapters)
            {
                builder.OpenElement(seq++, "h" + headerLevel);
                builder.AddAttribute(seq++, "id", chapter.Id);
                builder.AddContent(seq++, chapter.Title ?? string.Empty);
                builder.CloseElement();

                // Use help-specific content renderer
                builder.AddContent(seq++, HelpContentRenderer.RenderChapterContent(chapter.Content, _placeholderTemplates));

                if (chapter?.Chapters is { Count: > 0 })
                {
                    var nextHeaderLevel = Math.Min(headerLevel + 1, 6); // Cap at h6
                    builder.AddContent(seq++, RenderChapter(chapter.Chapters, nextHeaderLevel));
                }
            }
        };
    }

    private Chapter? GetSelectedTopChapter()
    {
        return ViewModel.HelpContentManual?.Chapters?.FirstOrDefault(c => c.Id == _selectedManualId);
    }

    private void GoToChapter(string chapterId)
    {
        // Update current chapter and current manual.
        // The link in the chapter menu navigates to the anchor on the page via the Href link.

        var comingFromStartPage = string.IsNullOrEmpty(_selectedManualId);

        if (!string.IsNullOrEmpty(chapterId))
        {
            // select chapter to indicate in chapter navigation
            _menuChapterId = _selectedChapterId = chapterId;
            // select current manual (top node)
            _selectedManualId = ViewModel.GetManualId(chapterId);

            // We need to scroll via script if we're coming from the start page (no manual content visible)
            _needsScroll = comingFromStartPage && _selectedChapterId != _selectedManualId;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_pageScript is not null)
        {
            await _pageScript.InvokeVoidAsync("stopListeningToLinkClicks", _richTextWrapper);
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;

        await _pageScript.SafeDisposeAsync();
        await _scrollScript.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }
}
