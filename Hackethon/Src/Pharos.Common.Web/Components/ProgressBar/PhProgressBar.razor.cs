using System.Timers;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.ProgressBar;

/// <summary>
/// This is the Pharos progress bar. Use it to display a static bar ratio (for example 15 days where max is 29),
/// or use it to show an animated progress (for example going from 0 to 100% in a given time frame).
/// </summary>
public partial class PhProgressBar : IDisposable
{
    /// <summary>
    /// Represents the max value of the progress bar (coresponding to 100%).
    /// </summary>
    [Parameter, EditorRequired] public int MaxValue { get; set; }

    /// <summary>
    /// Represents the current value, i.e. the position of the bar. Ignored during animation.
    /// </summary>
    [Parameter, EditorRequired] public int Value { get; set; }

    /// <summary>
    /// Display the current value as text on top of the progress bar.
    /// Currently not functioning for animated progress bar.
    /// </summary>
    [Parameter] public bool ShowValueText { get; set; }

    /// <summary>
    /// The text representing the unit in singular, for instance "day".
    /// </summary>
    [Parameter] public string UnitTextSingular { get; set; } = "";

    /// <summary>
    /// The text representing the unit in plural, for instance "days".
    /// </summary>
    [Parameter] public string UnitTextPlural { get; set; } = "";

    /// <summary>
    /// Time in seconds for the progress bar to reach the end, starting from 0.
    /// This property has no effect in static use.
    /// </summary>
    [Parameter] public int AnimateDuration { get; set; }

    /// <summary>
    /// Sets progress bar to the disabled styling.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Event invoked when animation is started.
    /// </summary>
    [Parameter]
    public EventCallback AnimationStarted { get; set; }

    /// <summary>
    /// Event invoked when animation has ended.
    /// </summary>
    [Parameter]
    public EventCallback AnimationDone { get; set; }

    // Value used for displaying text
    private int _displayValue;

    private bool _animationRunning;
    private string? ProgressStyle { get; set; }

    // Timer used for animation.
    private readonly System.Timers.Timer _timer = new(20);

    protected string Classname => new CssBuilder("ph-progress")
    .AddClass("ph-progress--disabled", Disabled).Build();

    /// <summary>
    /// Sets the position of the bar when not in animation mode
    /// </summary>
    protected override Task OnParametersSetAsync()
    {
        // Avoid setting values during animation
        if (_animationRunning)
        {
            return base.OnParametersSetAsync();
        }

        _displayValue = Value;

        // Set width of the bar according to the current value
        var valuePercent = (int)Math.Round(Value * 100f / MaxValue);
        ProgressStyle = $"width: {valuePercent}%;";

        return base.OnParametersSetAsync();
    }

    /// <summary>
    /// Start function for animated progress bar.
    /// </summary>
    public async Task RunAnimation()
    {
        var startTime = new DateTime();

        // Prepare event handlerto be used during animation
        ElapsedEventHandler? elapsedEventHandler = null;
        elapsedEventHandler = (source, e) =>
        {
            // Update value displayed as text.
            _displayValue = (int)(MaxValue * (e.SignalTime - startTime).TotalMilliseconds / (AnimateDuration * 1000));

            // End animation when max value is reached
            if (_displayValue >= MaxValue)
            {
                _timer.Stop();
                _displayValue = MaxValue;
                AnimationDone.InvokeAsync();
                _animationRunning = false;

                // Unsubscribe
                _timer.Elapsed -= elapsedEventHandler;
            }
            InvokeAsync(StateHasChanged);
        };

        // Reset the bar.
        ProgressStyle = $"width: 0%;";
        await Task.Yield();

        // Start the animation
        await AnimationStarted.InvokeAsync();
        _animationRunning = true;
        startTime = DateTime.Now;
        _timer.Elapsed += elapsedEventHandler;
        _timer.Start();

        // Set the bar to 100%, with linear transition for animated effect.
        ProgressStyle = $"width: 100%; transition: {AnimateDuration}s linear width;";
    }

    public void Dispose()
    {
        Value = 0;
        ProgressStyle = null;
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}