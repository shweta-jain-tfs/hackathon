namespace Pharos.Client.App.Parameters.Common.Models;

public class ParameterUpdatePackageVersionDataSource(HttpClient httpClient)
{
    public async Task<string?> GetVersion()
    {
        var client = new ParametersClient(httpClient);
        return await client.GetParameterUpdatePackageVersionAsync();
    }
}