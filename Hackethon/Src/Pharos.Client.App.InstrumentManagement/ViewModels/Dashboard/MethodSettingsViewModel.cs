using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;

public class MethodSettingsViewModel : EntityListViewModel<InstrumentMethod>, IDisposable
{
    private readonly IEntityDataService<InstrumentMethod> _dataService;
    private readonly ICurrentInstrumentNotificationProvider _notificationProvider;

    public MethodSettingsViewModel(
        IEntityDataService<InstrumentMethod> dataService,
        ICurrentInstrumentNotificationProvider notificationProvider
        ) : base(dataService)
    {
        _dataService = dataService;

        _notificationProvider = notificationProvider;
        _notificationProvider.MethodsChanged += NotificationProviderOnMethodsChanged;
    }

    private async void NotificationProviderOnMethodsChanged(object? sender, EventArgs e)
    {
        await base.LoadAsync();
    }

    public async Task ChangeActive(InstrumentMethod instrumentMethod)
    {
        await _dataService.UpdateAsync(instrumentMethod);
    }

    public async Task ChangeRunOptions(InstrumentMethod instrumentMethod)
    {
        await _dataService.UpdateAsync(instrumentMethod);
    }

    public void Dispose()
    {
        _notificationProvider.MethodsChanged -= NotificationProviderOnMethodsChanged;
        GC.SuppressFinalize(this);
    }
}