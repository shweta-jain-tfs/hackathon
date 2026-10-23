namespace Pharos.Client.App.Identity.Models;

public sealed class PharosModuleInformationDataSource(HttpClient httpClient) : ISingletonDataSource<ModuleInformation>
{
    public async Task<ModuleInformation> GetAsync()
    {
        var client = new CurrentInstrumentClient(httpClient);
        return await client.GetInstrumentModuleInformationAsync();
    }
}