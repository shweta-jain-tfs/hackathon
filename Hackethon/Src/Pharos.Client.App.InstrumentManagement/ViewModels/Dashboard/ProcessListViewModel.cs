using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;

public class ProcessListViewModel : VirtualGridViewModel<ProcessListItem>
{
    public event Action? OnTimerTick;
    private readonly ICurrentInstrumentNotificationProvider _notificationProvider;
    private readonly TimerService _timerService;

    public ProcessListViewModel(
        IEntityDataSource<ProcessListItem> dataSource,
        ICurrentInstrumentNotificationProvider notificationProvider,
        TimerService timerService,
        IExceptionHandlerService exceptionHandler
        ) : base(dataSource, exceptionHandler)
    {
        _notificationProvider = notificationProvider;

        _timerService = timerService;
        _timerService.OnTimerTick += TimerTick;
    }

    /// <summary>
    /// Re-render component every 15 seconds.
    /// </summary>
    private void TimerTick()
    {
        OnTimerTick?.Invoke();
    }

    private void NotificationProviderOnProcessListUpdated(object? sender, EventArgs e)
    {
        // All properties considered changed.
        OnViewRefreshNeeded();
    }

    protected override void SubscribeToNotifications()
    {
        base.SubscribeToNotifications();
        _notificationProvider.ProcessListUpdated += NotificationProviderOnProcessListUpdated;
    }

    protected override void UnsubscribeFromNotifications()
    {
        _notificationProvider.ProcessListUpdated -= NotificationProviderOnProcessListUpdated;
        base.UnsubscribeFromNotifications();
    }
}