using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.InstrumentManagement.Models.Dashboard;

public sealed class CurrentInstrumentEnvironmentDataSource(HttpClient httpClient) : ISingletonDataSource<InstrumentEnvironment>
{
    public async Task<InstrumentEnvironment> GetAsync()
    {
        var client = new CurrentInstrumentClient(httpClient);
        return await client.GetEnvironmentAsync();
    }
}