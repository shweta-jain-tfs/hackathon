using Pharos.Client.App.InstrumentManagement.Models.Dashboard;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;

public class HumidityViewModel : LoadingViewModel, IDisposable
{
    private readonly CurrentInstrumentEnvironmentDataSource _dataSource;
    private readonly ICurrentInstrumentNotificationProvider _notificationProvider;

    public HumidityViewModel(CurrentInstrumentEnvironmentDataSource dataSource, ICurrentInstrumentNotificationProvider notificationProvider)
    {
        _dataSource = dataSource;
        _notificationProvider = notificationProvider;
        _notificationProvider.InstrumentEnvironmentChanged += OnInstrumentEnvironmentChanged;
        Environment.HumidityList = new List<SensorValue>();
    }

    public SensorStatus Status { get; private set; }

    public InstrumentEnvironment Environment { get; private set; } = new();

    private void OnInstrumentEnvironmentChanged(object? sender, EventArgs e)
    {
        _ = ReloadEnvironmentAsync();
    }

    protected override async Task DoLoadAsync()
    {
        await ReloadEnvironmentAsync();
    }

    private async Task ReloadEnvironmentAsync()
    {
        Environment = await _dataSource.GetAsync();
        Status = SensorStatusHelper.GetSensorStatus(Environment.HumidityList);
        OnPropertyChanged(nameof(Status));
    }

    public void Dispose()
    {
        _notificationProvider.InstrumentEnvironmentChanged -= OnInstrumentEnvironmentChanged;
        GC.SuppressFinalize(this);
    }
}