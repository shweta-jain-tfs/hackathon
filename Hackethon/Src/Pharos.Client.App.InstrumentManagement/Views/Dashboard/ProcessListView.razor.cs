using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class ProcessListView : IDisposable
{
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private ProcessListViewModel ViewModel { get; set; } = null!;

    protected override void OnInitialized()
    {
        ViewModel.OnTimerTick += TimerTick;
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    /// <summary>
    /// Re-render component every 15 seconds.
    /// </summary>
    private void TimerTick()
    {
        StateHasChanged();
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.OnTimerTick -= TimerTick;
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}