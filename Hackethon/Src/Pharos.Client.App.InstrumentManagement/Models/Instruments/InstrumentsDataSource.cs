using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.InstrumentManagement.Models.Instruments;

internal sealed class InstrumentsDataSource(HttpClient httpClient) : IEntityDataSource<Instrument>
{
    public async Task<DataSourceResult<Instrument>> GetAsync(DataSourceRequest request)
    {
        var client = new InstrumentsClient(httpClient);
        var result = await client.GetAllAsync();

        return new DataSourceResult<Instrument>(result.ToList(), result.Count);
    }

    public Task<Instrument> GetByIdAsync(int entityId)
    {
        // used by notification provider
        var client = new InstrumentsClient(httpClient);
        return client.GetInstrumentAsync(entityId);
    }
}