using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models.SolidWasteDrawer;

/// <summary>
/// Data source implementation for solid waste drawer
/// </summary>
public class SolidWasteDrawerDataSource : ISolidWasteDrawerDataSource
{
    private readonly HttpClient _httpClient;

    public SolidWasteDrawerDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SolidWasteDrawerDetails> GetWasteDrawerDetailsAsync()
    {
        var client = new SolidWasteDrawerClient(_httpClient);
        return await client.GetWasteDrawerDetailsAsync();
    }

    public async Task UnlockSolidWasteDrawerAsync()
    {
        var client = new CurrentInstrumentClient(_httpClient);
        await client.UnlockSolidWasteDrawerAsync();
    }
}
