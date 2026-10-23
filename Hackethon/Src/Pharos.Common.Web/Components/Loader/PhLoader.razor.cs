using System.Timers;
using Pharos.Common.Web.Utilities;
using Timer = System.Timers.Timer;

namespace Pharos.Common.Web.Components.Loader;

public partial class PhLoader
{
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string? InitialText { get; set; }
    [Parameter] public string? AlternateText { get; set; }

    private string? _text = "";
    private Timer? _timer;
    private int _timerInterval;
    private int _alternate;
    private bool _visible;
    private bool _showText;

    private string CssClass => new CssBuilder("ph-loader-overlay")
        .AddClass("ph-loader-overlay--disable", !_visible)
        .Build();

    protected override void OnParametersSet()
    {
        if (IsLoading)
        {
            StartLoader();
        }
        else
        {
            StopLoader();
        }
    }

    protected override void OnInitialized()
    {
        StartLoader();
    }

    private void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        var invokeStateChange = false;

        if (_timerInterval == 0)
        {
            _visible = IsLoading;
            invokeStateChange = true;
        }
        else if (_timerInterval == 2)
        {
            _showText = true;
            _text = InitialText;
            invokeStateChange = true;
        }
        else if (_timerInterval > 2 && _timerInterval % 4 == 0)
        {
            _text = _alternate % 2 == 0 ? AlternateText : InitialText;
            _alternate++;
            invokeStateChange = true;
        }

        if (invokeStateChange)
        {
            InvokeAsync(StateHasChanged);
        }

        _timerInterval++;
    }

    private void StartLoader()
    {
        if (_timer?.Enabled == true)
        {
            return;
        }

        _text = InitialText;

        // reset interval to allow the timer to be restarted after it has been stopped
        _timerInterval = 0;

        _timer = new Timer();
        _timer.Elapsed += OnTimedEvent;
        _timer.Interval = 1000;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    private void StopLoader()
    {
        _visible = false;
        _timer?.Stop();
        _timer?.Dispose();

        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}