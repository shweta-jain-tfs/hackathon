using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.InstrumentManagement.Models.Instruments;

public sealed class InstrumentDataService : IEntityDataService<Instrument>
{
    private readonly HttpClient _httpClient;

    public InstrumentDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<int> CreateAsync(Instrument value)
    {
        throw new NotImplementedException("");
    }

    public async Task DeleteAsync(int entityId)
    {
        var client = new InstrumentsClient(_httpClient);
        await client.DeleteInstrumentAsync(entityId);
    }

    public Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        throw new NotImplementedException("");
    }

    public async Task<DataSourceResult<Instrument>> GetAsync(DataSourceRequest request)
    {
        var client = new InstrumentsClient(_httpClient);

        var result = await client.GetAllAsync();

        return new DataSourceResult<Instrument>(result.ToList(), result.Count);
    }

    public Task<Instrument> GetByIdAsync(int entityId)
    {
        var client = new InstrumentsClient(_httpClient);
        return client.GetInstrumentAsync(entityId);
    }

    public async Task UpdateAsync(Instrument value)
    {
        var client = new InstrumentsClient(_httpClient);
        var command = value.ToUpdateInstrumentCommand();
        await client.UpdateInstrumentAsync(value.Id, command);
    }
}