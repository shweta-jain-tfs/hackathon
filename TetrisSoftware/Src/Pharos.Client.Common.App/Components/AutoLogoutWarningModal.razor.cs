using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.Common.App.Components;

public partial class AutoLogoutWarningModal : ComponentBase, IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IDateTime DateTime { get; set; } = null!;

    [Parameter] public DateTimeOffset LogoutAt { get; set; }

    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    private int RemainingSeconds => Math.Max(0, (int)Math.Ceiling((LogoutAt - DateTime.UtcNow).TotalSeconds));

    /// <summary>
    /// Starts countdown updates using the service's logout deadline.
    /// </summary>
    protected override void OnInitialized()
    {
        _ = RunCountdownAsync();
    }

    /// <summary>
    /// Refreshes the countdown once per second until the modal is closed.
    /// </summary>
    private async Task RunCountdownAsync()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Stops countdown updates when the warning modal closes.
    /// </summary>
    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}
