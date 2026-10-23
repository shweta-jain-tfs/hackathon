using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Logs.ViewModels.Errors;

public class SystemErrorsListViewModel : VirtualGridViewModel<SystemErrorSummary>
{
    public SystemErrorsListViewModel(
        IEntityDataSource<SystemErrorSummary> dataSource,
        IEntityNotificationProvider<SystemErrorSummary> notificationProvider,
        IExceptionHandlerService exceptionHandler) : base(dataSource, exceptionHandler, notificationProvider)
    {
    }
}
