using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.Requestors.ViewModels;

public class RequestorListViewModel : VirtualGridViewModel<RequestorSummary>
{
    private readonly IEntityDataService<RequestorSummary> _dataService;

    public RequestorListViewModel(
        IEntityDataService<RequestorSummary> dataService,
        IExceptionHandlerService exceptionHandler,
        IEntityNotificationProvider<RequestorSummary> notificationProvider) : base(dataService, exceptionHandler, notificationProvider)
    {
        _dataService = dataService;
    }

    public async Task<DeleteResponse> DeleteRequestors(int[] entityIds)
    {
        return await _dataService.DeleteAsync(entityIds);
    }
}