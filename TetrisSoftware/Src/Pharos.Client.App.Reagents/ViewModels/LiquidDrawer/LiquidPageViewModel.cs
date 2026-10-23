using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.ViewModels.LiquidDrawer;

public class LiquidPageViewModel : LoadingViewModel, IDisposable
{
    private readonly ITaskCardRefreshNotificationProvider _taskCardRefreshNotificationProvider;
    private readonly ICurrentInstrumentTaskCardItemSource _taskCardItemSource;

    public LiquidDrawerViewModel LiquidDrawerViewModel { get; set; }
    public LiquidLegendViewModel LegendViewModel => LiquidDrawerViewModel.LegendViewModel;

    public LiquidPageViewModel(
        LiquidDrawerViewModel liquidDrawerViewModel,
        ITaskCardRefreshNotificationProvider taskCardRefreshNotificationProvider,
        ICurrentInstrumentTaskCardItemSource taskCardItemSource)
    {
        LiquidDrawerViewModel = liquidDrawerViewModel;
        _taskCardRefreshNotificationProvider = taskCardRefreshNotificationProvider;
        _taskCardItemSource = taskCardItemSource;
    }

    public void Dispose()
    {
        _taskCardRefreshNotificationProvider.RefreshNeeded -= OnTaskCardsRefreshNeeded;
        LiquidDrawerViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task DoLoadAsync()
    {
        _taskCardRefreshNotificationProvider.RefreshNeeded += OnTaskCardsRefreshNeeded;

        await LiquidDrawerViewModel.LoadAsync();
        await UpdateTaskCardLegendAsync();
    }

    public void ClearLegendSelection()
    {
        LiquidDrawerViewModel.ClearLegendSelection();
    }

    private async void OnTaskCardsRefreshNeeded(object? sender, EventArgs e)
    {
        await UpdateTaskCardLegendAsync();
    }

    private async Task UpdateTaskCardLegendAsync()
    {
        var taskCards = await _taskCardItemSource.GetAsync();
        LegendViewModel.UpdateTaskCardLegend(taskCards);
    }
}
