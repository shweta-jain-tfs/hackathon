namespace Pharos.Client.App.Identity.Models;

public sealed class PharosInformationDataSource(HttpClient httpClient) : ISingletonDataSource<PharosInformation>
{
    public async Task<PharosInformation> GetAsync()
    {
        var client = new PharosInformationClient(httpClient);
        return await client.GetPharosInformationAsync();
    }
}