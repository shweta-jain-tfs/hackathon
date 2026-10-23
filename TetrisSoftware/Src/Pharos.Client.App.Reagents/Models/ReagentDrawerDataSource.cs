using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models;

internal class ReagentDrawerDataSource : IReagentDrawerDataSource
{
    private readonly HttpClient _httpClient;

    public ReagentDrawerDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReagentDrawer> GetReagentDrawerDetailsAsync()
    {
        var client = new ReagentDrawerClient(_httpClient);
        return await client.GetReagentDrawerDetailsAsync();
    }

    public async Task UnlockReagentDrawerAsync()
    {
        var client = new CurrentInstrumentClient(_httpClient);
        await client.UnlockReagentDrawerAsync();
    }

    public async Task<ReagentContentDetail> GetReagentContentDetailAsync(string? barcode, string? reagentUdi)
    {
        var client = new ReagentDrawerClient(_httpClient);
        return await client.GetReagentContentDetailAsync(barcode, reagentUdi);
    }
}
