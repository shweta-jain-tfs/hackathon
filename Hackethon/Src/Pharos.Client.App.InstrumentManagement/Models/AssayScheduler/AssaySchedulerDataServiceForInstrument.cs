using Pharos.Client.Common.Models;

namespace Pharos.Client.App.InstrumentManagement.Models.AssayScheduler;

public sealed class AssaySchedulerDataServiceForInstrument(HttpClient httpClient)
{
    /// <summary>
    /// Set all assay schedulers to active/inactive.
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public async Task SetAllAssaySchedulersActiveAsync(bool isActive)
    {
        var client = new AssaySchedulerClient(httpClient);
        await client.SetAllActiveAsync(isActive);
    }

    /// <summary>
    /// Update the assay scheduler for one instrument.
    /// </summary>
    /// <param name="instrument"></param>
    /// <returns></returns>
    public async Task UpdateByInstrumentIdAsync(Instrument instrument)
    {
        var client = new AssaySchedulerClient(httpClient);

        var command = new UpdateAssaySchedulerCommand
        {
            IsActive = instrument.AssaySchedulerIsActive,
            PausedUntilDate = instrument.AssaySchedulerIsPausedUntilDate
        };
        await client.UpdateAssaySchedulerAsync(instrument.Id, command);
    }
}