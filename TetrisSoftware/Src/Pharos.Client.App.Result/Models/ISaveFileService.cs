namespace Pharos.Client.App.Result.Models;
public interface ISaveFileService<TQuery>
{
    Task SaveTestResultFileAsync(TQuery query);
}
