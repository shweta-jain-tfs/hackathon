using Pharos.Client.Common.Models;

namespace Pharos.Client.App.LoadList.Models;

public interface ILoadListDataSource
{
    Task<ICollection<LoadListBottle>> GetBottlesAsync();
    Task<ICollection<LoadListCarrier>> GetCarriersAsync();
    Task<ICollection<LoadListCartridge>> GetCartridgesAsync();
    Task<ICollection<LoadListDilutionPlate>> GetDilutionPlatesAsync();
    Task<ICollection<LoadListSampleTube>> GetSampleTubesAsync();
    Task<int> GetReagentLoadersCountAsync(IEnumerable<string>? instrumentNumbers = null, IEnumerable<LoadListStatus>? statuses = null);
}

public class LoadListDataSource : ILoadListDataSource
{
    private readonly HttpClient _httpClient;

    public LoadListDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ICollection<LoadListBottle>> GetBottlesAsync()
    {
        var client = new LoadListClient(_httpClient);
        return await client.GetLoadListBottlesAsync();
    }

    public async Task<ICollection<LoadListCarrier>> GetCarriersAsync()
    {
        var client = new LoadListClient(_httpClient);
        return await client.GetLoadListCarriersAsync();
    }

    public async Task<ICollection<LoadListCartridge>> GetCartridgesAsync()
    {
        var client = new LoadListClient(_httpClient);
        return await client.GetLoadListCartridgesAsync();
    }

    public async Task<ICollection<LoadListDilutionPlate>> GetDilutionPlatesAsync()
    {
        var client = new LoadListClient(_httpClient);
        return await client.GetLoadListDilutionPlatesAsync();
    }

    public async Task<ICollection<LoadListSampleTube>> GetSampleTubesAsync()
    {
        var client = new LoadListClient(_httpClient);
        return await client.GetLoadListSampleTubesAsync();
    }
    public async  Task<int> GetReagentLoadersCountAsync(IEnumerable<string>? instrumentNumbers = null, IEnumerable<LoadListStatus>? statuses = null)
    {
        var client = new LoadListClient(_httpClient);
        return await client.GetReagentLoadersCountAsync(instrumentNumbers, statuses);
    }
}