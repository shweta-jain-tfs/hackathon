using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.InstrumentManagement.Models.AssayScheduler;

internal sealed class AssaySchedulerDataService(HttpClient httpClient) : IEntityDataService<Common.Models.AssayScheduler>
{
    public async Task<DataSourceResult<Common.Models.AssayScheduler>> GetAsync(DataSourceRequest request)
    {
        var client = new AssaySchedulerClient(httpClient);
        var result = await client.GetAssaySchedulersAsync();

        return new DataSourceResult<Common.Models.AssayScheduler>(result.ToList(), result.Count);
    }

    public Task<Common.Models.AssayScheduler> GetByIdAsync(int entityId)
    {
        var client = new AssaySchedulerClient(httpClient);
        return client.GetByIdAsync(entityId);
    }

    public Task<int> CreateAsync(Common.Models.AssayScheduler value)
    {
        throw new NotImplementedException("");
    }

    public Task DeleteAsync(int entityId)
    {
        throw new NotImplementedException("");
    }

    public Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        throw new NotImplementedException("");
    }

    public Task UpdateAsync(Common.Models.AssayScheduler value)
    {
        throw new NotImplementedException("");
    }
}