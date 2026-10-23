using Pharos.Client.App.InstrumentManagement.Models.AssayScheduler;
using Pharos.Client.App.InstrumentManagement.Models.Instruments;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Instruments;

public sealed class InstrumentsViewModel(
    IEntityDataService<Instrument> dataService,
    AssaySchedulerDataServiceForInstrument assaySchedulerDataServiceForInstrument,
    IEntityNotificationProvider<Instrument> notificationProvider,
    ITestResultDataService resultDataService) : DynamicEntityListViewModel<Instrument>(dataService, notificationProvider)
{
    public async Task DeleteInstrument(int id)
    {
        await dataService.DeleteAsync(id);
    }

    public async Task<int> GetTestResultsCount(string serialNumber)
    {
        return await resultDataService.GetTestResultsCount(serialNumber);
    }

    public async Task SetAllAssaySchedulersActive(bool isActive)
    {
        await assaySchedulerDataServiceForInstrument.SetAllAssaySchedulersActiveAsync(isActive);
    }

    public async Task UpdateAssaySchedule(Instrument instrument)
    {
        await assaySchedulerDataServiceForInstrument.UpdateByInstrumentIdAsync(instrument);
    }

    public async Task UpdateInstrument(Instrument instrument)
    {
        await dataService.UpdateAsync(instrument);
    }
}
public enum InstrumentSortOption
{
    Name,
    Status
}
