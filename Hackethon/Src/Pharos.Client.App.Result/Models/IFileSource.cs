namespace Pharos.Client.App.Result.Models;
public interface IFileSource<TQuery>
{
    Task<FileDownloadInfo> GetFileAsync(TQuery query);
}
