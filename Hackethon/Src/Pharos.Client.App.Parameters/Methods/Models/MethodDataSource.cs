using System.Globalization;
using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.Common.ViewModels;

namespace Pharos.Client.App.Parameters.Methods.Models;

public interface IMethodDataSource : IPagedDataSource<Method>, IDetailsPageDataService<MethodDetails>, IExportParameters<GetParameterExportToClientQuery>;

public class MethodDataSource(HttpClient httpClient) : IMethodDataSource
{
    public async Task<MethodDetails> GetByIdAsync(int entityId)
    {
        var client = new MethodsClient(httpClient);
        return await client.GetMethodDetailsAsync(entityId);
    }

    public async Task UpdateAsync(MethodDetails value)
    {
        var client = new MethodsClient(httpClient);
        var command = value.ToUpdateMethodCommand();
        await client.UpdateMethodAsync(value.Id, command);
    }

    public async Task<DataSourceResult<Method>> GetAsync(DataSourceRequest request)
    {
        var client = new MethodsClient(httpClient);
        var result = await client.GetMethodsAsync();
        return new DataSourceResult<Method>(result.ToList(), result.Count);
    }

    public async Task<FileDownloadInfo> SaveFileAsync(GetParameterExportToClientQuery query)
    {
        var client = new MethodsClient(httpClient);
        var file = await client.GetParametersExportAsync(query);
        var fileName = CreateSuggestedExportName();
        return new FileDownloadInfo(fileName, file.Stream);
    }

    private static string CreateSuggestedExportName()
    {
        var fileName = $"ParameterExportPackage-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.zip";
        return fileName;
    }

    public Task<int> CreateAsync(MethodDetails value)
    {
        throw new NotImplementedException();
    }
}
