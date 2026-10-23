namespace Pharos.Client.App.Result.Models;

public interface IAnalyticalRunResultDataSource : IPagedDataSource<AnalyticalRunTestResult>
{
    Task<DataSourceResult<AnalyticalRunTestResult>> GetAsync(DataSourceRequest request, int analyticalRunId);
    Task<int> GetTestResultIndexAsync(Guid testOrderId, int analyticalRunId, DataSourceRequest request);
}