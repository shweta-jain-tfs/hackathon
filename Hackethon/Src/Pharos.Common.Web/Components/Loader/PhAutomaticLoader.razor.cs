namespace Pharos.Common.Web.Components.Loader;

/// <summary>
/// Represents an automatic loader component that manages loading states, progress indicators, and text cycling for asynchronous operations.
/// Implements IDisposable to ensure proper cleanup of resources.
/// </summary>
public partial class PhAutomaticLoader : IDisposable
{
    /// <summary>
    /// Gets or sets the injected LoaderService instance used for managing loader states.
    /// </summary>
    [Inject] private LoaderService LoaderService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the initial text to display when the loader is active.
    /// </summary>
    [Parameter] public string? InitialText { get; set; }

    /// <summary>
    /// Gets or sets the alternate text to display for text cycling when progress is not shown.
    /// </summary>
    [Parameter] public string? AlternateText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show progress indicators.
    /// </summary>
    [Parameter] public bool ShowProgress { get; set; }

    private string? _text = "";
    private string? _progressText = "";
    private int _intervalTick;
    private int _alternate;
    private bool _visible;
    private bool _showText;
    private bool _showProgressBar;
    private double _progressPercentage;
    private readonly HashSet<Guid> _activeCalls = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _delayTokens = new();
    private readonly Dictionary<Guid, (int current, int total)> _progressData = new();
    private readonly HashSet<Guid> _progressEnabledCalls = [];

    /// <summary>
    /// Initializes the component by subscribing to LoaderService events.
    /// </summary>
    protected override void OnInitialized()
    {
        LoaderService.OnStart += OnStart;
        LoaderService.OnStop += OnStop;
        LoaderService.OnProgressUpdate += OnProgressUpdate;

        LoaderService.OnStartProgress += OnStartProgress;
        LoaderService.OnStopProgress += OnStop;
        LoaderService.OnHideVisual += OnHideVisualRequested;
    }

    /// <summary>
    /// Handles the start of a loader operation for the specified call ID.
    /// Initiates delayed activation to show the loader after a threshold.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    private void OnStart(Guid callId)
    {
        _activeCalls.Add(callId);

        // Create a per-call delayed activation task
        var cts = new CancellationTokenSource();
        _delayTokens[callId] = cts;

        _ = DelayedActivateAsync(callId, cts.Token);
    }

    /// <summary>
    /// Handles the start of a progress-enabled loader operation for the specified call ID.
    /// Sets the progress text and initiates delayed activation.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    /// <param name="text">The text to display for the progress operation.</param>
    private void OnStartProgress(Guid callId, string? text)
    {
        _activeCalls.Add(callId);
        _progressEnabledCalls.Add(callId); // Automatically enable progress for ShowProgress

        _progressText = text;

        // Create a per-call delayed activation task
        var cts = new CancellationTokenSource();
        _delayTokens[callId] = cts;

        _ = DelayedActivateAsync(callId, cts.Token, true);
    }

    /// <summary>
    /// Asynchronously delays the activation of the loader and manages the periodic update loop.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    /// <param name="ct">The cancellation token to monitor for cancellation.</param>
    /// <param name="isProgressbar">Indicates whether this is a progress bar operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DelayedActivateAsync(Guid callId, CancellationToken ct, bool isProgressbar = false)
    {
        try
        {
            // 1 second threshold
            if (!isProgressbar)
            {
                await Task.Delay(1000, ct);
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (!_activeCalls.Contains(callId))
            {
                return;
            }

            // Show loader
            _visible = true;
            _text = GetDisplayText(callId);
            _showProgressBar = ShouldShowProgressBar(callId);
            _progressPercentage = GetProgressPercentage(callId);
            _intervalTick = 0;
            _alternate = 0;
            _showText = false;

            RequestRender();

            // Start a lightweight periodic update loop (every 1s)
            while (!ct.IsCancellationRequested && _activeCalls.Contains(callId))
            {
                await Task.Delay(1000, ct);
                if (ct.IsCancellationRequested || !_activeCalls.Contains(callId))
                {
                    break;
                }

                UpdateTextCycle(callId);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    /// <summary>
    /// Handles progress updates for the specified call ID.
    /// Updates the progress data and refreshes the UI if the loader is visible.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    /// <param name="current">The current progress value.</param>
    /// <param name="total">The total progress value.</param>
    private void OnProgressUpdate(Guid callId, int current, int total)
    {
        if (!_activeCalls.Contains(callId))
        {
            return;
        }

        _progressData[callId] = (current, total);

        if (_visible)
        {
            _text = GetDisplayText(callId);
            _showProgressBar = ShouldShowProgressBar(callId);
            _progressPercentage = GetProgressPercentage(callId);
            RequestRender();
        }
    }

    /// <summary>
    /// Determines whether the progress bar should be shown for the specified call ID.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    /// <returns>True if the progress bar should be shown; otherwise, false.</returns>
    private bool ShouldShowProgressBar(Guid callId)
    {
        var shouldShowProgress = ShowProgress || _progressEnabledCalls.Contains(callId);
        return shouldShowProgress && _progressData.ContainsKey(callId);
    }

    /// <summary>
    /// Calculates the progress percentage for the specified call ID.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    /// <returns>The progress percentage as a double.</returns>
    private double GetProgressPercentage(Guid callId)
    {
        if (_progressData.TryGetValue(callId, out var progress) && progress.total > 0)
        {
            return Math.Round((double)progress.current / progress.total * 100, 0);
        }
        return 0;
    }

    /// <summary>
    /// Gets the display text for the specified call ID, considering progress and alternation.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    /// <returns>The text to display.</returns>
    private string GetDisplayText(Guid callId)
    {
        var shouldShowProgress = ShowProgress || _progressEnabledCalls.Contains(callId);

        if (shouldShowProgress && _progressData.TryGetValue(callId, out var progress) && progress.total > 0)
        {
            //var percentage = Math.Round((double)progress.current / progress.total * 100, 1);

            if (!string.IsNullOrEmpty(_progressText))
            {
                return _progressText;
            }

            // When showing progress, always use InitialText as the base text without alternating
            return $"{InitialText}";
        }

        var output = _alternate % 2 == 0 || string.IsNullOrEmpty(AlternateText) ? InitialText : AlternateText;
        return string.IsNullOrEmpty(output) ? "" : output;
    }

    /// <summary>
    /// Updates the text cycle for the specified call ID, handling alternation and progress updates.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    private void UpdateTextCycle(Guid callId)
    {
        var changed = false;
        var shouldShowProgress = ShowProgress || _progressEnabledCalls.Contains(callId);

        if (_intervalTick == 1)
        {
            _showText = true;
            _text = GetDisplayText(callId);
            _showProgressBar = ShouldShowProgressBar(callId);
            _progressPercentage = GetProgressPercentage(callId);
            changed = true;
        }
        else if (_intervalTick > 1 && _intervalTick % 4 == 0)
        {
            // Only alternate text if NOT showing progress
            if (!shouldShowProgress || !_progressData.ContainsKey(callId))
            {
                _alternate++;
            }
            _text = GetDisplayText(callId);
            _showProgressBar = ShouldShowProgressBar(callId);
            _progressPercentage = GetProgressPercentage(callId);
            changed = true;
        }

        if (changed)
        {
            RequestRender();
        }

        _intervalTick++;
    }

    /// <summary>
    /// Handles the stop of a loader operation for the specified call ID.
    /// Cleans up resources and hides the loader if no active calls remain.
    /// </summary>
    /// <param name="callId">The unique identifier for the loader call.</param>
    private void OnStop(Guid callId)
    {
        _activeCalls.Remove(callId);
        _progressData.Remove(callId);
        _progressEnabledCalls.Remove(callId);

        if (_delayTokens.Remove(callId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        // If no active calls remain, hide loader
        if (_activeCalls.Count == 0)
        {
            HideLoader();
        }
    }

    /// <summary>
    /// Hides the loader and resets all related state variables.
    /// </summary>
    private void HideLoader()
    {
        _visible = false;
        _showText = false;
        _showProgressBar = false;
        _progressPercentage = 0;
        _text = "";
        _intervalTick = 0;
        _alternate = 0;
        _progressData.Clear();
        _progressEnabledCalls.Clear();
        RequestRender();
    }

    /// <summary>
    /// Requests a UI render update by invoking StateHasChanged asynchronously.
    /// </summary>
    private void RequestRender() => _ = InvokeAsync(StateHasChanged);

    /// <summary>
    /// Handles the cancel action by calling the LoaderService's Cancel method.
    /// </summary>
    private void OnCancel()
    {
        LoaderService.Cancel();
    }

    // hide loader, keep active state for potential resume
    private void OnHideVisualRequested()
    {
        _visible = false;
        RequestRender();
    }

    /// <summary>
    /// Disposes of the component, unsubscribing from events and cleaning up resources.
    /// </summary>
    public void Dispose()
    {
        LoaderService.OnStart -= OnStart;
        LoaderService.OnStop -= OnStop;
        LoaderService.OnProgressUpdate -= OnProgressUpdate;

        LoaderService.OnStartProgress -= OnStartProgress;
        LoaderService.OnStopProgress -= OnStop;
        LoaderService.OnHideVisual -= OnHideVisualRequested;

        foreach (var cts in _delayTokens.Values)
        {
            try { cts.Cancel(); cts.Dispose(); } catch { /* ignore */ }
        }
        _delayTokens.Clear();
        _activeCalls.Clear();
        _progressData.Clear();
        _progressEnabledCalls.Clear();

        GC.SuppressFinalize(this);
    }
}