using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.ViewModels.SolidWasteDrawer;

/// <summary>
/// ViewModel for the waste page
/// </summary>
public class SolidWastePageViewModel : LoadingViewModel, IDisposable
{
    private readonly ITaskCardRefreshNotificationProvider _taskCardRefreshNotificationProvider;
    private readonly ICurrentInstrumentTaskCardItemSource _taskCardItemSource;

    public SolidWasteDrawerViewModel WasteDrawerViewModel { get; set; }
    public SolidWasteLegendViewModel SolidWasteLegendViewModel { get; }

    public SolidWastePageViewModel(
        SolidWasteDrawerViewModel wasteDrawerViewModel,
        SolidWasteLegendViewModel solidWasteLegendViewModel,
        ITaskCardRefreshNotificationProvider taskCardRefreshNotificationProvider,
        ICurrentInstrumentTaskCardItemSource taskCardItemSource)
    {
        WasteDrawerViewModel = wasteDrawerViewModel;
        SolidWasteLegendViewModel = solidWasteLegendViewModel;
        _taskCardRefreshNotificationProvider = taskCardRefreshNotificationProvider;
        _taskCardItemSource = taskCardItemSource;
    }

    public void Dispose()
    {
        _taskCardRefreshNotificationProvider.RefreshNeeded -= OnTaskCardsRefreshNeeded;
        WasteDrawerViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task DoLoadAsync()
    {
        _taskCardRefreshNotificationProvider.RefreshNeeded += OnTaskCardsRefreshNeeded;

        await WasteDrawerViewModel.LoadAsync();
        await UpdateTaskCardLegendAsync();
    }

    private async void OnTaskCardsRefreshNeeded(object? sender, EventArgs e)
    {
        await UpdateTaskCardLegendAsync();
    }

    private async Task UpdateTaskCardLegendAsync()
    {
        var taskCards = await _taskCardItemSource.GetAsync();
        SolidWasteLegendViewModel.UpdateTaskCardLegend(taskCards);
    }
}
