using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.InstrumentManagement.Models.Dashboard;

internal sealed class ProcessListDataSource : IEntityDataSource<ProcessListItem>
{
    private readonly HttpClient _httpClient;

    public ProcessListDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ProcessListItem> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public async Task<DataSourceResult<ProcessListItem>> GetAsync(DataSourceRequest request)
    {
        var client = new CurrentInstrumentClient(_httpClient);

        var result = await client.GetProcessListAsync(null, request.StartIndex, request.Count, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<ProcessListItem>(result.Items.ToList(), result.TotalCount);
    }
}