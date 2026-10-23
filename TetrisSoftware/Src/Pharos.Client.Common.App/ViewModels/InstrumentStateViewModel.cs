using Pharos.Client.Common.App.Helpers;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.App.State.CurrentInstrument.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Health;

namespace Pharos.Client.Common.App.ViewModels;

internal sealed class InstrumentStateViewModel : ViewModelBase, IDisposable
{
    private const string _assayStartsInInfoKey = "InstrumentStateHelper_Info_AssayStartsIn";

    private readonly ICurrentInstrumentViewModel _instrumentModel;
    private readonly ICurrentInstrumentDataService _currentInstrumentDataService;
    private readonly IStringLocalizer<Translations> _loc;
    public event Action? OnStateRowsChanged;
    public string? RunInformationRow1 { get; private set; }
    public string? RunInformationRow2 { get; private set; }
    public bool ShowSkipTodayLink { get; private set; }

    // Fallback periodic refresh source.
    // Needed when no instrument events are raised, so the view can still refresh
    // and detect when countdown mode should start/stop.
    private readonly TimerService _timerService;

    private Timer? _countdownTimer;
    public InstrumentStateViewModel(
        ICurrentInstrumentViewModel instrumentModel,
        ICurrentInstrumentDataService currentInstrumentDataService,
        TimerService timerService,
        IStringLocalizer<Translations> loc)
    {
        _instrumentModel = instrumentModel;
        _currentInstrumentDataService = currentInstrumentDataService;
        _timerService = timerService;
        _loc = loc;

        _instrumentModel.OnCurrentInstrumentChange += LoadStatusRows;
        _instrumentModel.OnProcessStatusChanged += LoadStatusRows;

        // Periodic safety net: refresh rows even when no model events arrive.
        _timerService.OnTimerTick += OnTimerServiceTick;
    }

    private void LoadStatusRows()
    {
        var rowData = InstrumentStateHelper.GetInstrumentStateRowsData(
            _instrumentModel.Enabled,
            _instrumentModel.State,
            _instrumentModel.NumberOfTests,
            _instrumentModel.TimeWhenNextStateReached,
            _instrumentModel.AssaySchedulerNextRunType,
            _instrumentModel.AssaySchedulerNextRun);

        UpdateCountdownTimer(rowData);

        // Keep Skip today visibility exactly aligned with AssayStartsIn row visibility.
        ShowSkipTodayLink = rowData.InfoKey == _assayStartsInInfoKey && rowData.TimeWhenNextStateReached.HasValue;
        (RunInformationRow1, RunInformationRow2) = InstrumentStateRowsFormatter.Format(rowData, _loc);

        OnStateRowsChanged?.Invoke();
    }

    /// <summary>
    /// Starts or stops the per-second countdown timer based on current row data.
    /// The timer is only active when the UI shows the "assay starts in" countdown.
    /// </summary>
    private void UpdateCountdownTimer(InstrumentStateRowsData rowData)
    {
        // Countdown should run only for the specific info row and only when a target time exists.
        var shouldRunCountdownTimer = rowData.InfoKey == _assayStartsInInfoKey && rowData.TimeWhenNextStateReached.HasValue;

        if (shouldRunCountdownTimer)
        {
            // Timer already running: nothing to change.
            if (_countdownTimer != null)
            {
                return;
            }

            // Align the first tick to the next full second for stable countdown display.
            var now = DateTime.Now;
            var dueTimeMs = 1000 - now.Millisecond;
            if (dueTimeMs <= 0)
            {
                dueTimeMs = 1000;
            }

            _countdownTimer = new Timer(OnCountdownTimerTick, null, dueTimeMs, 1000);
            return;
        }

        // Countdown no longer needed: stop and clear the timer.
        _countdownTimer?.Dispose();
        _countdownTimer = null;
    }

    /// <summary>
    /// Handles per-second countdown timer ticks and refreshes state rows.
    /// </summary>
    private void OnCountdownTimerTick(object? state)
    {
        LoadStatusRows();
    }

    /// <summary>
    /// Handles periodic TimerService ticks when the per-second countdown timer is not active.
    /// This keeps the UI updated in quiet periods where no instrument events are raised.
    /// </summary>
    private void OnTimerServiceTick()
    {
        if (_countdownTimer != null)
        {
            return;
        }

        LoadStatusRows();
    }

    public HealthStatus MapHealthStatus()
    {
        return HealthStatusMapper.Map(_instrumentModel.SignalTower.LightState, _instrumentModel.State);
    }

    /// <summary>
    /// Skips the next scheduled automatic assay start and updates countdown UI immediately.
    /// </summary>
    public async Task SkipNextScheduledAssayStart()
    {
        await _currentInstrumentDataService.SkipNextScheduledAssayStart();

        // Hide countdown/link immediately in UI, then reload to sync with server updates.
        ShowSkipTodayLink = false;
        RunInformationRow2 = null;
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        OnStateRowsChanged?.Invoke();

        LoadStatusRows();
    }

    public void Dispose()
    {
        _timerService.OnTimerTick -= OnTimerServiceTick;
        _countdownTimer?.Dispose();
        _countdownTimer = null;
        _instrumentModel.OnCurrentInstrumentChange -= LoadStatusRows;
        _instrumentModel.OnProcessStatusChanged -= LoadStatusRows;
        _instrumentModel.Dispose();
    }
}