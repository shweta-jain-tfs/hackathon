using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Sample.ViewModels;

public class TraysViewModel(
    ISampleTrayDataService sampleTrayDataService,
    IEntityNotificationProvider<SampleTray, string> notificationProvider,
    ISampleTrayPositionNotificationProvider sampleTrayPositionNotificationProvider,
    InstrumentFilterViewModel instrumentFilterModel,
    IStringLocalizer<Translations> loc,
    IExceptionHandlerService exceptionHandler)
    : VirtualGridViewModel<SampleTray, string>(sampleTrayDataService, exceptionHandler, notificationProvider)
{
    private readonly IEntityNotificationProvider<SampleTray, string> _notificationProvider = notificationProvider;

    public async Task<(bool Success, List<string> ClearedTrayIds)> ClearTraysAsync(string[] entityIds)
    {
        var clearedTrayIds = await sampleTrayDataService.ClearTraysAsync(entityIds);
        var success = entityIds.All(clearedTrayIds.Contains);

        return (success, clearedTrayIds);
    }

    public async Task<(bool Success, List<string> DeletedTrayIds)> DeleteTraysAsync(string[] entityIds)
    {
        var deletedTrayIds = await sampleTrayDataService.DeleteTraysAsync(entityIds);
        var success = entityIds.All(deletedTrayIds.Contains);

        return (success, deletedTrayIds);
    }

    public async Task<Dictionary<string, string>> GetTrayLocationFilterAsync()
    {
        var trayLocations = new Dictionary<string, string>
        {
            { SampleConstants.NotLoaded, loc["TraysPage_TrayLocationFilter_NotLoaded"] }
        };

        await instrumentFilterModel.LoadAsync();
        var instruments = instrumentFilterModel.FilterDataSourceSerial;
        foreach (var instrument in instruments)
        {
            if (instrument.Value == loc["TraysPage_TrayLocationFilter_NotLoaded"])
            {
                continue;
            }
            trayLocations[instrument.Key] = instrument.Value;
        }

        return trayLocations;
    }

    protected override void SubscribeToNotifications()
    {
        base.SubscribeToNotifications();
        sampleTrayPositionNotificationProvider.FreePositionAndErrorCountUpdated += OnSampleTrayPositionCountUpdated;
        _notificationProvider.Updated += OnSampleTrayUpdated;
    }

    protected override void UnsubscribeFromNotifications()
    {
        sampleTrayPositionNotificationProvider.FreePositionAndErrorCountUpdated -= OnSampleTrayPositionCountUpdated;
        _notificationProvider.Updated -= OnSampleTrayUpdated;
        base.UnsubscribeFromNotifications();
    }

    private Task OnSampleTrayPositionCountUpdated()
    {
        OnViewRefreshNeeded();
        return Task.CompletedTask;
    }

    private Task OnSampleTrayUpdated(string id)
    {
        OnViewRefreshNeeded();
        return Task.CompletedTask;
    }
}