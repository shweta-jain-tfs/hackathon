namespace Pharos.Client.App.Parameters.Common.Models;
public class ImportParametersDataService(HttpClient httpClient)
{
    /// <summary>
    /// Abort the import process.
    /// </summary>
    /// <returns></returns>
    public async Task AbortImportPackage()
    {
        var paramsClient = new ParametersClient(httpClient);
        await paramsClient.CancelMethodUpdateAsync();
    }

    /// <summary>
    /// Start the import process.
    /// </summary>
    /// <returns></returns>
    public async Task StartPackageImport()
    {
        var paramsClient = new ParametersClient(httpClient);
        await paramsClient.StartMethodUpdateAsync();
    }

    public async Task<Guid> UploadMethodUpdatePackageFileAsync(FileParameter fileParameter)
    {
        var paramsClient = new ParametersClient(httpClient);
        return await paramsClient.UploadMethodUpdatePackageFileAsync(fileParameter);
    }

    public async Task<string> GetMethodUpdatePackageDescriptionAsync(Guid id)
    {
        var paramsClient = new ParametersClient(httpClient);
        return await paramsClient.GetMethodUpdatePackageDescriptionAsync(id);
    }

    public async Task<bool> IsParameterJobInProgressAsync()
    {
        var paramsClient = new ParametersClient(httpClient);
        return await paramsClient.IsParameterJobInProgressAsync();
    }
}
