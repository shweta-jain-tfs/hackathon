using Pharos.Client.Common.Models;

namespace Pharos.Client.App.InstrumentManagement.Models.Dashboard;

public sealed class LabCommunityConnectionDataService(HttpClient httpClient)
{
    private readonly LabCommunityClient _client = new(httpClient);

    public async Task<LabCommunityConnectionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _client.GetStatusAsync(cancellationToken);
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _client.IsLabCommunityEnabledAsync(cancellationToken);
    }
}
