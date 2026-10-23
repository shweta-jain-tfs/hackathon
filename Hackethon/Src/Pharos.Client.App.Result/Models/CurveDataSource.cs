namespace Pharos.Client.App.Result.Models;

public interface ICurveDataSource : IEntityDataSource<CurvePresentation>
{
    Task<CurvePresentation?> GetByRunAsync(int entityId);
    Task UpdateOperatorComment(CalibratorTestResult result);
    Task ReRunCurve(int analyticalRunId);
    Task<ICollection<AvailableCurve>> GetAvailableCurves(int analyticalRunId);
    Task ChangeCurveForAnalyticalRun(int selectedCurveId, int analyticalRunId);
    Task AcceptCurve(int analyticalRunId);
}

internal sealed class CurveDataSource(HttpClient httpClient) : ICurveDataSource
{
    public async Task<CurvePresentation> GetByIdAsync(int entityId)
    {
        var client = new CurvesClient(httpClient);
        return await client.GetCurveAsync(entityId);
    }

    public async Task<CurvePresentation?> GetByRunAsync(int entityId)
    {
        var client = new CurvesClient(httpClient);
        try
        {
            return await client.GetCurveByARAsync(entityId);
        }
        catch (ApiException e)
        {
            if (e.StatusCode == 204)
            {
                return null;
            }
            throw new ApiException("The HTTP status code of the response was not expected (" + e.StatusCode + ").", e.StatusCode, e.Response, e.Headers, null);
        }
    }
    
    public Task<DataSourceResult<CurvePresentation>> GetAsync(DataSourceRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateOperatorComment(CalibratorTestResult value)
    {
        var client = new CurvesClient(httpClient);
        var command = new UpdateTestResultCommand
        {
            Id = value.Id,
            OperatorComment = value.Comment
        };
        await client.UpdateCurveAsync(command);
    }

    public async Task ReRunCurve(int analyticalRunId)
    {
        var client = new CurvesClient(httpClient);

        var command = new RerunCalibratorCurveCommand
        {
            AnalyticalRunId = analyticalRunId
        };

        await client.ReRunCurveAsync(command);
    }

    public async Task<ICollection<AvailableCurve>> GetAvailableCurves(int analyticalRunId)
    {
        var client = new CurvesClient(httpClient);
        return await client.GetAvailableCurvesAsync(analyticalRunId);
    }

    public async Task ChangeCurveForAnalyticalRun(int selectedCurveId, int analyticalRunId)
    {
        var client = new CurvesClient(httpClient);
        var command = new ChangeCurveForAnalyticalRunCommand()
        {
            SelectedCurveId = selectedCurveId,
            AnalyticalRunId = analyticalRunId
        };
        await client.ChangeCurveForAnalyticalRunAsync(command);
    }

    public Task AcceptCurve(int analyticalRunId)
    {
        var client = new CurvesClient(httpClient);
        var command = new AcceptCurveFromFlaggedCurveControlCommand()
        {
            AnalyticalRunId = analyticalRunId
        };
        return client.AcceptCurveFromFlaggedCurveControlAsync(command);
    }
}
