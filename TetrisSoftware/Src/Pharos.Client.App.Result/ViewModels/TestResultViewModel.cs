using Pharos.Client.App.Result.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Result.ViewModels;

internal sealed class TestResultViewModel(
    IAnalyticalRunResultDataSource analyticalRunResultDataSource,
    ISampleTestResultDataService sampleResultDataService,
    IQualityControlDataSource qualityControlDataSource,
    IExceptionHandlerService exceptionHandler) : VirtualGridViewModel<AnalyticalRunTestResult>(analyticalRunResultDataSource, exceptionHandler)
{
    public async ValueTask<GridItemsProviderResult<AnalyticalRunTestResult>> ProvideGridItems(
        GridItemsProviderRequest<AnalyticalRunTestResult> request, int analyticalRunId)
    {
        var dsRequest = ToDataSourceRequest(request);
        var dataSourceResult = await analyticalRunResultDataSource.GetAsync(dsRequest, analyticalRunId);
        return GridItemsProviderResult.From(dataSourceResult.Items, dataSourceResult.TotalItemCount);
    }

    public Task<int> GetTestOrderIndexAsync(Guid testOrderId, int analyticalRunId)
    {
        return analyticalRunResultDataSource.GetTestResultIndexAsync(testOrderId, analyticalRunId, new DataSourceRequest
        {
            Filters = Array.Empty<DataSourceFilter>(),
            CancellationToken = CancellationToken.None
        });
    }

    public async Task<QualityControlTestResult> GetQualityControlTestResult(int testResultId)
    {
        return await qualityControlDataSource.GetByIdAsync(testResultId);
    }

    public async Task UpdateQualityControlAsync(QualityControlTestResult resultData)
    {
        await qualityControlDataSource.UpdateQualityControlAsync(resultData);
    }

    public Task<int> ApproveTestResultsAsync(ICollection<Guid> testOrderIds)
    {
        return sampleResultDataService.ApproveTestResults(testOrderIds);
    }

    public Task<int> RejectTestResultsAsync(ICollection<Guid> testOrderIds, bool retest, int? instrumentDilution = null)
    {
        return sampleResultDataService.ApproveTestResults(testOrderIds, true, retest, instrumentDilution);
    }

    public Task<bool> GetTestResultsPendingReflex(int analyticalRunId)
    {
        return sampleResultDataService.GetTestResultsPendingReflex(analyticalRunId);
    }

    public async Task<ICollection<PendingReflex>> GetPendingReflexes(ICollection<Guid> testOrderIds)
    {
        return await sampleResultDataService.GetPendingReflexes(testOrderIds);
    }

    public async Task<bool> HasManualReflexDefinitions()
    {
        return await sampleResultDataService.HasManualReflexDefinitions();
    }

    public async Task<StatusType> GetOverallStatusType(int analyticalRunId)
    {
        return await sampleResultDataService.GetOverallStatusForTestResults(analyticalRunId);
    }

    public async Task UpdateSampleTestResult(SampleTestResult resultData)
    {
        await sampleResultDataService.UpdateAsync(resultData);
    }
    public async Task<bool> HasManualReflexDefinitionsInRequestCopy(int analyticalRunId)
    {
        return await sampleResultDataService.HasManualReflexDefinitionsInRequestCopy(analyticalRunId);
    }

    public async Task<int> ResetTestResults(List<Guid> testOrderIds)
    {
        return await sampleResultDataService.ResetTestResults(testOrderIds);
    }

    public async Task<SampleTestResult> GetSampleTestResult(int testResultId)
    {
        return await sampleResultDataService.GetSampleTestResult(testResultId);
    }
}