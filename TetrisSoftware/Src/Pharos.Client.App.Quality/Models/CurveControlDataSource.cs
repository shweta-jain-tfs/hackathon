namespace Pharos.Client.App.Quality.Models;

public interface ICurveControlResultDataSource
{
    public Task<CurveControlChart> GetCurveControls(string instrumentNumber, string methodName, string curveControlName, int lastDays);
    public Task<ICollection<string>> GetCurveControlNames(string methodName);
}

internal sealed class CurveControlDataSource(HttpClient httpClient) : ICurveControlResultDataSource
{
    public async Task<CurveControlChart> GetCurveControls(string instrumentNumber, string methodName, string curveControlName, int lastDays)
    {
        var client = new TestResultsClient(httpClient);
        return await client.GetCurveControlsForQualityAsync(instrumentNumber, methodName, curveControlName, lastDays);
    }

    public async Task<ICollection<string>> GetCurveControlNames(string methodName)
    {
        var client = new MethodsClient(httpClient);
        return await client.GetMethodCurveControlNamesAsync(methodName);
    }
}