using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.Methods.Models;

namespace Pharos.Client.App.Parameters.Common.ViewModels;

public record FileDownloadInfo(string Name, Stream Stream);

public class ExportParameterPackageViewModel(IMethodDataSource exportParameters, ImportParametersDataService importParameters)
{
    public async Task<FileDownloadInfo> ExportPackageToZip()
    {
        return await exportParameters.SaveFileAsync(new GetParameterExportToClientQuery()
        {
            MethodIds = [],
            ExportPackage = true
        });
    }
    
    public async Task StartPackageImport()
    {
       await importParameters.StartPackageImport();
    }

    public async Task AbortPackageImport()
    {
        await importParameters.AbortImportPackage();
    }

    public async Task<Guid> UploadMethodUpdatePackageFileAsync(FileParameter fileParameter)
    {
        return await importParameters.UploadMethodUpdatePackageFileAsync(fileParameter);
    }

    public async Task<string> GetMethodUpdatePackageDescriptionAsync(Guid id)
    {
        return await importParameters.GetMethodUpdatePackageDescriptionAsync(id);
    }

    public async Task<bool> IsParameterJobInProgress()
    {
        return await importParameters.IsParameterJobInProgressAsync();
    }
}