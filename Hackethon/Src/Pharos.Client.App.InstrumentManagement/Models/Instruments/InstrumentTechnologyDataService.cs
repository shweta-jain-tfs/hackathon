using Pharos.Client.Common.Models;

namespace Pharos.Client.App.InstrumentManagement.Models.Instruments;

public interface IInstrumentTechnologyDataService
{
    Task<List<InstrumentTechnology>> GetAsync(int instrumentId);
    Task UpdateAsync(int instrumentId, IEnumerable<InstrumentTechnology> technologies);
}

public sealed class InstrumentTechnologyDataService(HttpClient httpClient) : IInstrumentTechnologyDataService
{
    public async Task<List<InstrumentTechnology>> GetAsync(int instrumentId)
    {
        var client = new InstrumentsClient(httpClient);
        var result = await client.GetInstrumentTechnologiesAsync(instrumentId);

        return result.ToList();
    }

    public async Task UpdateAsync(int instrumentId, IEnumerable<InstrumentTechnology> technologies)
    {
        var client = new InstrumentsClient(httpClient);
        var command = new UpdateInstrumentTechnologiesCommand
        {
            Technologies = technologies
                .Select(x => new UpdateInstrumentTechnology
                {
                    Technology = x.Technology,
                    IsSelected = x.IsSelected
                })
                .ToList()
        };

        await client.UpdateInstrumentTechnologiesAsync(instrumentId, command);
    }
}
