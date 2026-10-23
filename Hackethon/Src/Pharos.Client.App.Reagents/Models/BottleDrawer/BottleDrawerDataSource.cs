using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models.BottleDrawer;

internal class BottleDrawerDataSource : IBottleDrawerDataSource
{
    private readonly HttpClient _httpClient;

    public BottleDrawerDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BottleDrawerDetails> GetBottleDrawerDetailsAsync()
    {
        var client = new BottleDrawerClient(_httpClient);
        return await client.GetBottleDrawerDetailsAsync();
    }

    public async Task<LoadingPosition> GetBottleLoadingPosition(string barcode)
    {
        var client = new BottleDrawerClient(_httpClient);
        return await client.GetLoadingPositionAsync(barcode);
    }
}
