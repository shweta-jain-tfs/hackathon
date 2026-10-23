using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.App.TaskCard.Models;

public class CurrentInstrumentTaskCardItemSource : ICurrentInstrumentTaskCardItemSource
{
    private readonly HttpClient _httpClient;

    public CurrentInstrumentTaskCardItemSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Common.Models.TaskCard>> GetAsync()
    {
        var client = new TaskCardClient(_httpClient);
        var items = await client.GetTaskCardsForCurrentInstrumentAsync();
        return items.ToList();
    }
}