using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models.LiquidDrawer;

internal class LiquidDrawerDataSource : ILiquidDrawerDataSource
{
    private readonly HttpClient _httpClient;

    public LiquidDrawerDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LiquidDrawerDetails> GetLiquidDrawerDetailsAsync()
    {
        var client = new LiquidDrawerClient(_httpClient);
        return await client.GetLiquidDrawerDetailsAsync();
    }

}