namespace Pharos.Client.App.Sample.Models;

internal class SampleTrayDetailsDataService(HttpClient httpClient) : ISampleTrayDetailsDataService
{
    private readonly SampleTrayDetailsClient _client = new(httpClient);

    public Task<string> CreateAsync(SampleTrayDetails value)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string entityId)
    {
        throw new NotImplementedException();
    }

    public Task<DeleteResponse> DeleteAsync(string[] entityIds)
    {
        throw new NotImplementedException();
    }

    public Task<DataSourceResult<SampleTrayDetails>> GetAsync(DataSourceRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<SampleTrayDetails> GetByIdAsync(string entityId)
    {
        return await _client.GetSampleTrayDetailsAsync(entityId);
    }

    public Task UpdateAsync(SampleTrayDetails value)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateSamplePositionAsync(string trayId, int tubePosition, string tubeBarcode, bool isHighPriority)
    {
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = trayId,
            TubePosition = tubePosition,
            TubeBarcode = tubeBarcode,
            HighPriority = isHighPriority
        };

        await _client.UpdateSampleTrayPositionAsync(trayId, command);
    }

    public async Task<string> GetSampleTrayLocationAsync(string trayId)
    {
        return await _client.GetSampleTrayLocationAsync(trayId);
    }
}
