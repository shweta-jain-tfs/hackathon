using Pharos.Client.Common.App.Extensions;

namespace Pharos.Client.Common.App.Layout.Instrument;

#pragma warning disable CS8618

public partial class PhNavigationHistory
{
    [Inject] private NavigationHistoryManager HistoryManager { get; set; }

    protected override Task OnParametersSetAsync()
    {
        // A way to signal that back/forward buttons should update.
        HistoryManager.NavigationHistoryEvent += (sender, args) => StateHasChanged();

        // Clear navigation history.
        HistoryManager.Clear();

        // Insert current route, so there is one after refresh.
        HistoryManager.InsertCurrentRoute();

        return base.OnParametersSetAsync();
    }

    /// <summary>
    /// Clicked on the "back" button.
    /// </summary>
    private void NavigateBack()
    {
        HistoryManager.NavigateBack();
    }

    /// <summary>
    /// Clicked on the "forward" button.
    /// </summary>
    private void NavigateForward()
    {
        HistoryManager.NavigateForward();
    }
}