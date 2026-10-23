using System.Timers;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;
using Timer = System.Timers.Timer;

namespace Pharos.Common.Web.Components.Toaster;

public partial class PhToaster : IDisposable
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public string Text { get; set; } = string.Empty;
    [Parameter] public ToastType Type { get; set; }
    [Parameter] public EventCallback<bool> ShowChanged { get; set; }

    private string CssClasses =>
        new CssBuilder("ph-toaster")
            .AddClass("ph-toaster--show", Show)
            .AddClass("ph-toaster--no-transition", !_useTransition)
            .AddClass("ph-toaster--indicator-alarm", Type == ToastType.Alarm)
            .AddClass("ph-toaster--indicator-warning", Type == ToastType.Warning)
            .AddClass("ph-toaster--indicator-information", Type == ToastType.Information)
            .AddClass("ph-toaster--indicator-confirmation", Type == ToastType.Confirmation)
            .Build();

    private Timer? _timer;
    private const int _toasterDisplayDuration = 5000;
    private bool _useTransition = true;

    protected override Task OnParametersSetAsync()
    {
        if (Show)
        {
            _useTransition = true;
            StateHasChanged();
            StartTimer();
        }

        return Task.CompletedTask;
    }

    private void StartTimer()
    {
        _timer?.Stop();
        _timer = new Timer(_toasterDisplayDuration);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = false;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        CloseToaster();
    }

    /// <summary>
    /// Close toaster.
    /// </summary>
    private void CloseToaster()
    {
        Show = false;
        ShowChanged.InvokeAsync(false);
        StateHasChanged();
    }

    /// <summary>
    /// Close toaster without transition.
    /// </summary>
    private void CloseToasterWitNoTransition()
    {
        _useTransition = false;
        CloseToaster();
    }

    private static StatusIconType GetStatusType(ToastType type)
    {
        return type switch
        {
            ToastType.Alarm => StatusIconType.Alarm,
            ToastType.Information => StatusIconType.PendingAction,
            ToastType.Warning => StatusIconType.Warning,
            _ => StatusIconType.Ok
        };
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static bool ShowIcon(ToastType type)
    {
        return type switch
        {
            ToastType.Confirmation => false,
            _ => true
        };
    }
}