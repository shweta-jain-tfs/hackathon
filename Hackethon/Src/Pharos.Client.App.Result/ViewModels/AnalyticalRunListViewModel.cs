using Pharos.Client.App.Result.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Result.ViewModels;

internal sealed class AnalyticalRunListViewModel(
    IAnalyticalRunDataSource dataSource,
    IEntityNotificationProvider<AnalyticalRun> notificationProvider,
    IFileSource<GetResultExportQuery> fileSource,
    IExceptionHandlerService exceptionHandler)
    : VirtualGridViewModel<AnalyticalRun>(dataSource, exceptionHandler, notificationProvider)
{
    public Task<FileDownloadInfo> GetResultExportAsync(ICollection<int> analyticalRunIds)
    {
        return fileSource.GetFileAsync(new GetResultExportQuery { AnalyticalRunIds = analyticalRunIds });
    }

    public async Task DeleteAnalyticalRuns(IEnumerable<int> analyticalRunIds)
    {
        await dataSource.DeleteAnalyticalRuns(analyticalRunIds);
    }
}