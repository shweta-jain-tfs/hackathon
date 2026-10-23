using Microsoft.Extensions.Logging;
using Pharos.Client.Common.App.Helpers;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.App.State.CurrentInstrument.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.Common.App.ViewModels;

public sealed class LockScreenViewModel : ViewModelBase, IDisposable
{
    private readonly ICurrentInstrumentViewModel _instrumentModel;
    private readonly ICurrentInstrumentDataService _currentInstrumentDataService;
    private readonly IStringLocalizer<Translations> _loc;
    private readonly ILogger<LockScreenViewModel> _logger;
    private bool _isPipettingStarted;
    private bool _isTestingStarted;

    private ProcessStatusSummary? _latestProcessStatus;

    public event Action? OnLockScreenTestInfoChanged;

    public IReadOnlyList<LockScreenStateInfoItem> StateInfoItems { get; private set; } = [];

    public LockScreenViewModel(
        ICurrentInstrumentViewModel instrumentModel,
        ICurrentInstrumentDataService currentInstrumentDataService,
        IStringLocalizer<Translations> loc,
        ILogger<LockScreenViewModel> logger)
    {
        _instrumentModel = instrumentModel;
        _currentInstrumentDataService = currentInstrumentDataService;
        _loc = loc;
        _logger = logger;

        _instrumentModel.OnCurrentInstrumentChange += HandleInstrumentStateChange;
        _instrumentModel.OnProcessStatusChanged += HandleProcessStatusChange;
    }

    public Task LoadAsync()
    {
        return LoadLockScreenStatusInfoAsync();
    }

    /// <summary>
    /// Handles instrument state changes by displaying current state information.
    /// Uses cached test count from last process status (authoritative source)
    /// and live state time from instrument model (most current for InfoRow.Until states).
    /// </summary>
    private void HandleInstrumentStateChange()
    {
        RebuildStateInfoItems();
    }

    private void HandleProcessStatusChange()
    {
        _ = LoadLockScreenStatusInfoAsync();
    }

    private async Task LoadLockScreenStatusInfoAsync()
    {
        try
        {
            var processStatus = await _currentInstrumentDataService.GetProcessStatusSummary();
            _latestProcessStatus = processStatus;
            RebuildStateInfoItems();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load lock screen status information.");
        }
    }

    private void RebuildStateInfoItems()
    {
        StateInfoItems = BuildStateInfoItems();
        OnLockScreenTestInfoChanged?.Invoke();
    }

    private List<LockScreenStateInfoItem> BuildStateInfoItems()
    {
        var numberOfTests = _latestProcessStatus?.RemainingResults ?? _instrumentModel.NumberOfTests;
        var stateItem = BuildInstrumentStateItem(numberOfTests, _instrumentModel.TimeWhenNextStateReached);
        if (_latestProcessStatus == null)
        {
            return [stateItem];
        }

        var processStatus = _latestProcessStatus;
        var items = new List<LockScreenStateInfoItem>();

        // Check if we have any valid timing data (protocol uses UnixEpoch when not available)
        var showPipettingTime = processStatus.LastPipettingTime > DateTimeOffset.UnixEpoch || _isPipettingStarted;
        var showTestingTime = processStatus.LastRemainingResultTime > DateTimeOffset.UnixEpoch || _isTestingStarted;

        // If no timing data, show only state
        if (!showPipettingTime && !showTestingTime)
        {
            return [stateItem];
        }

        var isInAssayState = IsAssayRelatedState(_instrumentModel.State);

        if (!isInAssayState)
        {
            // Show only state when not in assay
            return [stateItem];
        }

        // 1. Sample section (LastPipettingTime)
        if (showPipettingTime)
        {
            _isPipettingStarted = true;
            items.Add(BuildTimeItem(LockScreenStateInfoItemType.Sample, processStatus.LastPipettingTime));
        }

        // 2. State section (InstrumentState + NumberOfTests)
        items.Add(stateItem);

        // 3. Test section (LastRemainingResultTime)
        if (showTestingTime)
        {
            _isTestingStarted = true;
            items.Add(BuildTimeItem(LockScreenStateInfoItemType.Test, processStatus.LastRemainingResultTime));
        }

        return items;
    }

    private LockScreenStateInfoItem BuildTimeItem(LockScreenStateInfoItemType type, DateTimeOffset targetTime)
    {
        var (headerKey, completedKey) = GetTranslationKeysForType(type);
        var timeDisplay = LockScreenTimeFormatter.FormatTime(targetTime, _loc);

        if (!timeDisplay.Hours.HasValue && !timeDisplay.Minutes.HasValue)
        {
            return BuildCompletedItem(type, _loc[completedKey].Value);
        }

        return new LockScreenStateInfoItem(
            type,
            HeaderText: _loc[headerKey].Value,
            PrimaryText: null,
            SecondaryText: null,
            Hours: timeDisplay.Hours,
            HoursUnit: timeDisplay.HoursUnit,
            Minutes: timeDisplay.Minutes,
            MinutesUnit: timeDisplay.MinutesUnit,
            TimeText: timeDisplay.SecondaryInfo);
    }

    private static (string headerKey, string completedKey) GetTranslationKeysForType(LockScreenStateInfoItemType type)
    {
        if (type == LockScreenStateInfoItemType.Sample)
        {
            return ("LockScreen_SamplesPipetted_In", "LockScreen_SamplesPipetted");
        }

        if (type == LockScreenStateInfoItemType.Test)
        {
            return ("LockScreen_TestsFinished_In", "LockScreen_TestsFinished");
        }

        throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    /// <summary>
    /// Builds the instrument state display item with test count and state timing.
    /// </summary>
    /// <param name="numberOfTests">Number of tests in queue (from process status).</param>
    /// <param name="timeWhenNextStateReached">Time when the current state will complete (nullable).</param>
     private LockScreenStateInfoItem BuildInstrumentStateItem(int numberOfTests, DateTimeOffset? timeWhenNextStateReached)
    {
        var rowData = InstrumentStateHelper.GetInstrumentStateRowsData(
            _instrumentModel.Enabled,
            _instrumentModel.State,
            numberOfTests,
            timeWhenNextStateReached,
            hidePrimingTestCount: true);

        var (stateText, testsText, infoText) = InstrumentStateRowsFormatter.FormatLockScreen(rowData, _loc);

        return new LockScreenStateInfoItem(
            LockScreenStateInfoItemType.InstrumentState,
            HeaderText: null,
            PrimaryText: stateText,
            SecondaryText: string.IsNullOrWhiteSpace(testsText) ? infoText : testsText,
            Hours: null,
            HoursUnit: null,
            Minutes: null,
            MinutesUnit: null,
            TimeText: null);
    }

    private static LockScreenStateInfoItem BuildCompletedItem(LockScreenStateInfoItemType type, string completedText)
    {
        return new LockScreenStateInfoItem(
            type,
            HeaderText: null,
            PrimaryText: completedText,
            SecondaryText: null,
            Hours: null,
            HoursUnit: null,
            Minutes: null,
            MinutesUnit: null,
            TimeText: null);
    }

    private static bool IsAssayRelatedState(InstrumentState state)
    {
        return state is InstrumentState.Assay
            or InstrumentState.AssayCmr
            or InstrumentState.AssayFinishing
            or InstrumentState.AssayFinishOrdered;
    }

    public void Dispose()
    {
        _instrumentModel.OnCurrentInstrumentChange -= HandleInstrumentStateChange;
        _instrumentModel.OnProcessStatusChanged -= HandleProcessStatusChange;
    }
}
