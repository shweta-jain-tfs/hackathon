namespace Pharos.Client.App.Result.Models;

public interface IMethodNameDataSource
{
    public Task<List<string>> GetMethodNamesAsync();
}
