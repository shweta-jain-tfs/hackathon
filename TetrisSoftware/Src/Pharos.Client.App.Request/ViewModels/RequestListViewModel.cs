using Pharos.Client.App.Request.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Request.ViewModels;

public class RequestListViewModel : VirtualGridViewModel<RequestSummary>
{
    private readonly IRequestSummaryDataService _dataService;
    private readonly IRequestSummaryNotificationProvider _summaryNotificationProvider;
    private readonly ISampleTubeNotificationProvider _sampleTubeNotificationProvider;

    public RequestListViewModel(
        IRequestSummaryDataService dataService,
        IExceptionHandlerService exceptionHandler,
        IRequestSummaryNotificationProvider notificationProvider,
        ISampleTubeNotificationProvider sampleTubeNotificationProvider) : base(dataService, exceptionHandler, notificationProvider)
    {
        _dataService = dataService;
        _summaryNotificationProvider = notificationProvider;
        _sampleTubeNotificationProvider = sampleTubeNotificationProvider;
    }

    public async Task<DeleteResponse> DeleteSamples(int[] entityIds)
    {
        return await _dataService.DeleteAsync(entityIds);
    }

    protected override void SubscribeToNotifications()
    {
        base.SubscribeToNotifications();
        _summaryNotificationProvider.RequestsDeleted += OnRequestsDeleted;
        _sampleTubeNotificationProvider.SampleTubeUpdated += OnSampleTubeUpdated;
    }

    protected override void UnsubscribeFromNotifications()
    {
        base.UnsubscribeFromNotifications();
        _summaryNotificationProvider.RequestsDeleted -= OnRequestsDeleted;
        _sampleTubeNotificationProvider.SampleTubeUpdated -= OnSampleTubeUpdated;
    }

    private void OnRequestsDeleted(object? sender, List<int> requestIds)
    {
        if (requestIds.Any(IsItemFetched))
        {
            OnViewRefreshNeeded();
        }
    }

    /// <summary>
    /// Refreshes the view when a visible request's sample tube changes.
    /// </summary>
    private void OnSampleTubeUpdated(object? sender, int requestId)
    {
        // Only refresh if the updated request is currently visible in the grid
        if (IsItemFetched(requestId))
        {
            OnViewRefreshNeeded();
        }
    }
}