namespace Pharos.Client.App.ServiceTools.Services;

public interface ILogDumpDataService
{
    Task<bool> IsCollectionInProgressAsync();
    Task<LogDumpCollectionResult> GetCollectionResultAsync();
    Task<bool> StartCollectionAsync(LogDumpCollectionRequest request);
    Task CancelCollectionAsync();
}

internal sealed class LogDumpDataService(HttpClient httpClient) : ILogDumpDataService
{
    public Task<bool> IsCollectionInProgressAsync()
    {
        var client = new LogDumpClient(httpClient);
        return client.IsLogDumpCollectionInProgressAsync();
    }

    public Task<LogDumpCollectionResult> GetCollectionResultAsync()
    {
        var client = new LogDumpClient(httpClient);
        return client.GetLogDumpCollectionResultAsync();
    }

    public Task<bool> StartCollectionAsync(LogDumpCollectionRequest request)
    {
        var client = new LogDumpClient(httpClient);
        return client.StartLogDumpCollectionAsync(request);
    }

    public Task CancelCollectionAsync()
    {
        var client = new LogDumpClient(httpClient);
        return client.CancelLogDumpCollectionAsync();
    }
}
