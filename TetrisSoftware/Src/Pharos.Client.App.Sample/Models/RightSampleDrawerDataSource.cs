namespace Pharos.Client.App.Sample.Models;

internal sealed class RightSampleDrawerDataSource : IRightSampleDrawerDataSource
{
    private readonly HttpClient _httpClient;

    public RightSampleDrawerDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task UnlockDrawerAsync()
    {
        var client = new CurrentInstrumentClient(_httpClient);
        await client.UnlockRightSampleDrawerAsync();
    }

    public async Task<SampleDrawer> GetDrawerDetailsAsync()
    {
        var client = new SampleDrawerClient(_httpClient);

        return await client.GetRightSampleDrawerDetailsAsync();
    }
}