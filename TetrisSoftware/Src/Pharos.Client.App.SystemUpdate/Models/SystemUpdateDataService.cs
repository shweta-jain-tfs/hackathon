using Pharos.Client.Common.Models;

namespace Pharos.Client.App.SystemUpdate.Models;

internal sealed class SystemUpdateDataService(HttpClient httpClient) : ISystemUpdateDataService
{
    private readonly SystemUpdateClient _client = new(httpClient);

    public async Task<List<string>> GetAvailableUpdates()
    {
        var updates = await _client.GetAvailableUpdatesAsync();

        return updates.ToList();
    }

    public Task InitiateUpdate()
    {
        return _client.InitiateUpdateAsync();
    }

    public Task AcknowledgeUpdate()
    {
        return _client.AcknowledgeUpdateAsync();
    }

    public Task<SystemUpdateOutcome> GetUnacknowledgedOutcome()
    {
        return _client.GetUnacknowledgedOutcomeAsync();
    }
}