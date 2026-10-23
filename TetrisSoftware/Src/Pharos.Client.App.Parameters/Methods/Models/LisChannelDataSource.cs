namespace Pharos.Client.App.Parameters.Methods.Models;

public interface ILisChannelDataSource
{
    Task<IEnumerable<LisChannel>> GetChannelsAsync();
}

internal sealed class LisChannelDataSource(HttpClient httpClient) : ILisChannelDataSource
{
    public async Task<IEnumerable<LisChannel>> GetChannelsAsync()
    {
        var client = new LisClient(httpClient);
        return await client.GetChannelsAsync();
    }
}