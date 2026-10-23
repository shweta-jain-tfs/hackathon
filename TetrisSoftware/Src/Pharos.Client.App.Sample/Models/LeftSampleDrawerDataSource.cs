namespace Pharos.Client.App.Sample.Models;

internal sealed class LeftSampleDrawerDataSource : ILeftSampleDrawerDataSource
{
    private readonly HttpClient _httpClient;

    public LeftSampleDrawerDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task UnlockDrawerAsync()
    {
        var client = new CurrentInstrumentClient(_httpClient);
        await client.UnlockLeftSampleDrawerAsync();
    }

    public async Task<SampleDrawer> GetDrawerDetailsAsync()
    {
        var client = new SampleDrawerClient(_httpClient);

        return await client.GetLeftSampleDrawerDetailsAsync();
    }
}