using Pharos.Client.App.Reagents.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Drawer;

namespace Pharos.Client.App.Reagents.ViewModels;
internal class ReagentPageViewModel : LoadingViewModel, IDisposable
{
    private readonly IReagentDrawerDataSource _reagentDrawerDataSource;
    private readonly ITaskCardRefreshNotificationProvider _taskCardRefreshNotificationProvider;
    private readonly ICurrentInstrumentTaskCardItemSource _taskCardItemSource;

    public ReagentDrawerViewModel ReagentDrawerViewModel { get; }
    public ReagentLegendViewModel ReagentLegendViewModel { get; }

    public ReagentPageViewModel(
        ReagentDrawerViewModel reagentDrawerViewModel,
        IReagentDrawerDataSource reagentDrawerDataSource,
        ReagentLegendViewModel reagentLegendViewModel,
        ITaskCardRefreshNotificationProvider taskCardRefreshNotificationProvider,
        ICurrentInstrumentTaskCardItemSource taskCardItemSource)
    {
        ReagentDrawerViewModel = reagentDrawerViewModel;
        _reagentDrawerDataSource = reagentDrawerDataSource;
        ReagentLegendViewModel = reagentLegendViewModel;
        _taskCardRefreshNotificationProvider = taskCardRefreshNotificationProvider;
        _taskCardItemSource = taskCardItemSource;
    }

    protected override async Task DoLoadAsync()
    {
        ReagentDrawerViewModel.LockStateChanged += OnReagentDrawerLockStateChanged;
        ReagentDrawerViewModel.ScanningStateChanged += OnReagentDrawerScanningStateChanged;
        ReagentDrawerViewModel.DrawerContentsChanged += OnDrawerContentsChanged;
        ReagentLegendViewModel.SelectedStatusChanged += OnLegendSelectionStatusChange;
        ReagentDrawerViewModel.RemoveLegendSelection += OnRemoveLegendSelection;
        _taskCardRefreshNotificationProvider.RefreshNeeded += OnTaskCardsRefreshNeeded;

        await ReagentDrawerViewModel.LoadAsync();
        await UpdateViewModel();
        await UpdateTaskCardLegendAsync();
    }

    public void Dispose()
    {
        ReagentDrawerViewModel.LockStateChanged -= OnReagentDrawerLockStateChanged;
        ReagentDrawerViewModel.ScanningStateChanged -= OnReagentDrawerScanningStateChanged;
        ReagentDrawerViewModel.DrawerContentsChanged -= OnDrawerContentsChanged;
        ReagentLegendViewModel.SelectedStatusChanged -= OnLegendSelectionStatusChange;
        ReagentDrawerViewModel.RemoveLegendSelection -= OnRemoveLegendSelection;
        _taskCardRefreshNotificationProvider.RefreshNeeded -= OnTaskCardsRefreshNeeded;
        GC.SuppressFinalize(this);
    }

    public void ClearLegendSelection()
    {
        ReagentLegendViewModel.ClearLegendSelection();
    }

    private async Task UpdateViewModel()
    {
        var drawerDetails = await _reagentDrawerDataSource.GetReagentDrawerDetailsAsync();
        ReagentDrawerViewModel.UpdateDrawer(drawerDetails);
    }

    private async void OnReagentDrawerLockStateChanged(object? sender, PhDrawerLockState lockState)
    {
        await UpdateViewModel();
    }

    private void OnReagentDrawerScanningStateChanged(object? sender, bool isScanningOngoing)
    {
        //TODO: Placeholder for future implementation
    }

    private void OnDrawerContentsChanged(object? sender, EventArgs e)
    {
        ReagentLegendViewModel.UpdateLegend(ReagentDrawerViewModel.ReagentLoaders, ReagentDrawerViewModel.DilutionPlateStacks, ReagentDrawerViewModel.NeedEmptyLoader);
    }

    private void OnLegendSelectionStatusChange()
    {
        ReagentDrawerViewModel.UpdateLegendStatus(ReagentLegendViewModel.SelectedLegendStatus);
    }

    private void OnRemoveLegendSelection()
    {
        ReagentLegendViewModel.ClearLegendSelection();
    }

    private async void OnTaskCardsRefreshNeeded(object? sender, EventArgs e)
    {
        await UpdateTaskCardLegendAsync();
    }

    private async Task UpdateTaskCardLegendAsync()
    {
        var taskCards = await _taskCardItemSource.GetAsync();
        ReagentLegendViewModel.UpdateTaskCardLegend(taskCards);
    }
}
