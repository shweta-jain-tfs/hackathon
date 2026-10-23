using Pharos.Client.App.Parameters.Common.ViewModels;

namespace Pharos.Client.App.Parameters.Common.Models;

public interface IExportParameters<in TQuery>
{
    Task<FileDownloadInfo> SaveFileAsync(TQuery query);
}