using Pharos.Client.Common.Models;

namespace Pharos.Client.App.InstrumentManagement.Models.AssayScheduler;

public sealed class AssaySchedulerDayDataService(HttpClient httpClient)
{
    // Update a specific assay scheduler day for an instrument.
    public async Task UpdateAsync(AssaySchedulerDay value)
    {
        var client = new AssaySchedulerClient(httpClient);
        var command = value.ToUpdateAssaySchedulerDayCommand();
        await client.UpdateAssaySchedulerDayAsync(value.Id, command);
    }
}