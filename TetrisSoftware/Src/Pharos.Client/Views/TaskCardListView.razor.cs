using Pharos.Client.App.TaskCard.ViewModels;
using Pharos.Common.Web.Utilities;
using Pharos.Contracts.Security;

namespace Pharos.Client.Views;

public partial class TaskCardListView : IDisposable
{
    [Inject] private TaskCardsLockScreenViewModel ViewModels { get; set; } = default!;

    [Parameter] public EventCallback<bool> OnHasCardsChanged { get; set; }

    private bool _isCentered;
    private bool _hasTaskCards = true;
    private bool _hasReportedInitialCount;
    private int _previousCount = -1;

    protected override Task OnInitializedAsync()
    {
        ViewModels.ViewRefreshNeeded += RefreshView;
        return ViewModels.LoadAsync();
    }

    private async void RefreshView(object? sender, EventArgs e)
    {
        var currentCount = ViewModels.Count;
        var hasCards = currentCount > 0;

        if (!_hasReportedInitialCount || _hasTaskCards != hasCards)
        {
            _hasReportedInitialCount = true;
            _hasTaskCards = hasCards;
            await OnHasCardsChanged.InvokeAsync(hasCards);
        }

        if (currentCount != _previousCount)
        {
            _previousCount = currentCount;
            _isCentered = currentCount <= 3;
        }

        await InvokeAsync(StateHasChanged);
    }

    private static string GetLoginUrlWithReturn(string originalUrl)
    {
        return $"{AccountRoutes.Login}?returnUrl={originalUrl}";
    }

    private string GetContentCssClass()
    {
        return new CssBuilder("ph-task-card-list-view")
        .AddClass("ph-task-card-list-view--centered", _isCentered)
        .AddClass("ph-task-card-list-view--hidden", !_hasTaskCards)
        .Build();
    }

    public void Dispose()
    {
        ViewModels.ViewRefreshNeeded -= RefreshView;
        ViewModels.Dispose();
        GC.SuppressFinalize(this);
    }
}