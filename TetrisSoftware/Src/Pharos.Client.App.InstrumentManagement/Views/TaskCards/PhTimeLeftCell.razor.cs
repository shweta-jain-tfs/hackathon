using Microsoft.AspNetCore.Components;
using Pharos.Client.Common.App.Helpers;
using Pharos.Client.Common.App.Services;
using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.InstrumentManagement.Views.TaskCards;

public partial class PhTimeLeftCell : IDisposable
{
    [Parameter] public DateTimeOffset? ExpiresAt { get; set; }
    [Parameter] public StatusType Status { get; set; }
    [Inject] private TimerService TimerService { get; set; } = default!;

    protected override void OnInitialized()
    {
        TimerService.OnTimerTick += OnTimerTick;
    }

    private string GetTimeLeft()
    {
        return ExpiresAt != null ? DateTimeHelper.GetTimeLeft(ExpiresAt.Value) : string.Empty;
    }

    private async void OnTimerTick()
    {
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        TimerService.OnTimerTick -= OnTimerTick;
        GC.SuppressFinalize(this);
    }
}