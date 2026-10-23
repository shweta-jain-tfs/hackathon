using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models.Inventory;
public interface IReagentDataSource
{
    Task UnloadReagentAsync(List<UnloadReagentRequestItem> unloadReagents);
}

internal class ReagentDataSource : IReagentDataSource
{
    private readonly InventoryClient _client;

    public ReagentDataSource(HttpClient httpClient)
    {
        _client = new InventoryClient(httpClient);
    }

    public async Task UnloadReagentAsync(List<UnloadReagentRequestItem> unloadReagents)
    {
        await _client.UnloadReagentAsync(unloadReagents);
    }
}
