namespace Pharos.Client.App.Result.Models;

public interface ICurveControlResultDataSource : IEntityDataSource<CurveControlTestResult>
{
    public int AnalyticalRunId { get; set; }
    public Task<CurveControlChart> GetCurveControls(int analyticalRunId, string curveControlName, int points);
    public Task RerunCurveControl(int analyticalRunId);
    Task UpdateCurveControlComment(CurveControlTestResult resultData);
}

internal sealed class CurveControlDataSource(HttpClient httpClient) : ICurveControlResultDataSource
{
    public int AnalyticalRunId { get; set; }

    public async Task<DataSourceResult<CurveControlTestResult>> GetAsync(DataSourceRequest request)
    {
        var client = new TestResultsClient(httpClient);
        var result = await client.GetCurveControlByArAsync(AnalyticalRunId);
        return new DataSourceResult<CurveControlTestResult>(result.ToList(), result.Count);
    }

    public async Task<CurveControlChart> GetCurveControls(int analyticalRunId, string curveControlName, int points)
    {
        var client = new TestResultsClient(httpClient);
        return await client.GetCurveControlForChartsAsync(analyticalRunId, curveControlName, points);
    }

    public Task<CurveControlTestResult> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public async Task RerunCurveControl(int analyticalRunId)
    {
        var client = new TestResultsClient(httpClient);

        await client.RerunCurveControlAsync(new RerunCurveControlCommand
        {
            AnalyticalRunId = analyticalRunId
        });
    }

    public async Task UpdateCurveControlComment(CurveControlTestResult resultData)
    {
        var client = new TestResultsClient(httpClient);
        await client.UpdateCurveControlCommentAsync(new UpdateCurveControlCommentCommand
        {
            Id = resultData.Id,
            Comment = resultData.Comment
        });
    }
}