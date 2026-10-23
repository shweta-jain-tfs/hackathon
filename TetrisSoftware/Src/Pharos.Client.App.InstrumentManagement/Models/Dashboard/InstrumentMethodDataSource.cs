using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.InstrumentManagement.Models.Dashboard;

public sealed class InstrumentMethodDataSource : IEntityDataService<InstrumentMethod>
{
    private readonly HttpClient _httpClient;

    public InstrumentMethodDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<InstrumentMethod> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public async Task<DataSourceResult<InstrumentMethod>> GetAsync(DataSourceRequest request)
    {
        var client = new CurrentInstrumentClient(_httpClient);
        var items = await client.GetMethodsAsync();

        return new DataSourceResult<InstrumentMethod>(items.ToList(), items.Count);
    }

    public Task<int> CreateAsync(InstrumentMethod value)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(InstrumentMethod value)
    {
        var client = new CurrentInstrumentClient(_httpClient);
        await client.UpdateAvailableMethodAsync(new UpdateInstrumentMethodCommand
        {
            InstrumentMethodId = value.Id,
            IsActive = value.IsActive,
            // Calibrator is set by the instrument, not by users.
            ForceCalibrator = value.ForceCalibrator,
            WhenToRun = value.WhenToRun
        });
    }

    public Task DeleteAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        throw new NotImplementedException();
    }
}
