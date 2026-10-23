namespace Pharos.Client.Common.App.Components;

public partial class ConnectionLostModal : ComponentBase, IDisposable
{
    [Inject] private IConnectionHealthService ConnectionHealthService { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    private string OfflineTime
    {
        get
        {
            var offlineTime = ConnectionHealthService.OfflineTime;
            if (offlineTime < TimeSpan.FromHours(1))
            {
                return $"{(int)offlineTime.TotalMinutes}:{offlineTime.Seconds:D2}";
            }
            else
            {
                return $"{offlineTime.Hours}:{offlineTime.Minutes:D2}:{offlineTime.Seconds:D2}";
            }
        }
    }

    protected override Task OnInitializedAsync()
    {
        return RunTimerAsync();
    }

    private async Task RunTimerAsync()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private void OnButtonClickServiceUi()
    {
        NavigationManager.NavigateTo("https://www.example.com/placeholder-for-serviceui", forceLoad: true);
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}