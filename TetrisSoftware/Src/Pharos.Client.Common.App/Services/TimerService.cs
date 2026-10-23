namespace Pharos.Client.Common.App.Services;

/// <summary>
/// Service that provides a timer which ticks every 15 seconds,
/// always aligned to 0, 15, 30, and 45 seconds of each minute (wall-clock aligned).
/// </summary>
public class TimerService : IDisposable
{
    // The underlying timer instance.
    private Timer? _timer;

    // Indicates if the timer is currently running.
    private bool _isRunning;

    /// <summary>
    /// Event that is triggered on every timer tick.
    /// </summary>
    public event Action? OnTimerTick;

    /// <summary>
    /// Constructor. Starts the timer automatically.
    /// </summary>
    public TimerService()
    {
        Start();
    }

    /// <summary>
    /// Starts the timer if it is not already running.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        ScheduleNextTick();
    }

    /// <summary>
    /// Stops the timer if it is running.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _isRunning = false;
    }

    /// <summary>
    /// Schedules the timer to fire at the next 0, 15, 30, or 45 second mark.
    /// This keeps the timer aligned to the wall clock.
    /// </summary>
    private void ScheduleNextTick()
    {
        var now = DateTime.Now;

        // Define the quarter points in a minute.
        int[] quarters = [0, 15, 30, 45, 60];

        // Find the next quarter after the current second.
        var nextQuarter = quarters.First(q => now.Second < q);

        // Calculate milliseconds until the next quarter.
        var msToNext = ((nextQuarter - now.Second) * 1000) - now.Millisecond;
        if (msToNext <= 0)
        {
            msToNext = 1000; // fallback to 1 second if calculation fails
        }

        // Create the timer if it doesn't exist yet.
        _timer ??= new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);

        // Schedule the timer to fire once at the calculated interval.
        _timer.Change(msToNext, Timeout.Infinite);
    }

    /// <summary>
    /// Called when the timer fires.
    /// Triggers the OnTimerTick event and schedules the next tick.
    /// </summary>
    private void TimerCallback(object? state)
    {
        OnTimerTick?.Invoke();

        if (!_isRunning)
        {
            return;
        }

        // Schedule the next tick to maintain wall-clock alignment.
        ScheduleNextTick();
    }

    /// <summary>
    /// Disposes the timer and releases resources.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;
        GC.SuppressFinalize(this);
    }
}