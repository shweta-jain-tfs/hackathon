using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.InstrumentManagement.Models.Dashboard;

public interface ILisConnectionDataService : IEntityDataSource<LisConnection>
{
    Task<List<LisConnectionSummary>> GetAllSummaryAsync();
    Task<LisConnectionSummary> GetSummaryByChannelIdAsync(int channelId);
    Task ResetStatisticsAsync(int channelId);
}

internal sealed class LisConnectionDataService(HttpClient httpClient) : ILisConnectionDataService
{
    public Task<LisConnection> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public async Task<DataSourceResult<LisConnection>> GetAsync(DataSourceRequest request)
    {
        var client = new LisClient(httpClient);
        var lisConnections = await client.GetAllAsync(new CancellationToken());
        return new DataSourceResult<LisConnection>(lisConnections.ToList(), lisConnections.Count);
    }

    public async Task<List<LisConnectionSummary>> GetAllSummaryAsync()
    {
        var client = new LisClient(httpClient);
        var lisConnectionSummaries = await client.GetAllSummaryAsync();
        return lisConnectionSummaries.ToList();
    }

    public async Task<LisConnectionSummary> GetSummaryByChannelIdAsync(int channelId)
    {
        var client = new LisClient(httpClient);
        var lisConnectionSummary = await client.GetSummaryAsync(channelId);
        return lisConnectionSummary;
    }


    public async Task ResetStatisticsAsync(int channelId)
    {
        var client = new LisClient(httpClient);
        await client.ResetStatisticsAsync(channelId, new CancellationToken());
    }
}