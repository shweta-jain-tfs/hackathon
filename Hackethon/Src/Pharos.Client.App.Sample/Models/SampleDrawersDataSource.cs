namespace Pharos.Client.App.Sample.Models;

internal sealed class SampleDrawersDataSource : IAllSampleDrawerDataSource
{
    private readonly HttpClient _httpClient;

    public SampleDrawersDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SampleDrawer>> GetDrawersDetailsAsync()
    {
        var client = new SampleDrawerClient(_httpClient);

        return (IReadOnlyList<SampleDrawer>)await client.GetAllSampleDrawersDetailsAsync();
    }
}