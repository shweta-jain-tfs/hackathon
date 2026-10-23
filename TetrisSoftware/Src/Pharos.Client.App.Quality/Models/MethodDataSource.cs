namespace Pharos.Client.App.Quality.Models;

public interface IMethodDataSource
{
    Task<ICollection<string>> GetActiveConjugateLots(string instrumentNumber, string methodName);
} 

internal sealed class MethodDataSource(HttpClient httpClient) : IMethodDataSource
{
    public async Task<ICollection<string>> GetActiveConjugateLots(string instrumentNumber, string methodName)
    {
        var client = new CurvesClient(httpClient);
        return await client.GetActiveConjugateLotsAsync(instrumentNumber, methodName);
    }
}
