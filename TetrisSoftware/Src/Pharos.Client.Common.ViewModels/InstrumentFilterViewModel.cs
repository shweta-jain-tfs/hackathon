using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Contracts;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// Shared view model that provides instrument filter values (serial number and display name) for dropdowns.
/// </summary>
/// <remarks>
/// Call <see cref="LoadAsync"/> to populate the filter data sources.
/// If the client runs on an instrument device, it also sets default filter values to the current instrument.
/// </remarks>
public sealed class InstrumentFilterViewModel : ViewModelBase, ILoadingViewModel, IDisposable
{
    private readonly IClientDeviceService _clientDevice;
    private readonly HttpClient _httpClient;
    private readonly IEntityNotificationProvider<Instrument> _instrumentNotifications;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public bool IsBusy
    {
        get;
        private set => SetField(ref field, value);
    }

    public bool NeedsReload
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public InstrumentFilterViewModel(IClientDeviceService clientDevice,
        HttpClient httpClient,
        IEntityNotificationProvider<Instrument> instrumentNotifications)
    {
        _clientDevice = clientDevice;
        _httpClient = httpClient;
        _instrumentNotifications = instrumentNotifications;
        _instrumentNotifications.Created += InstrumentListUpdated;
        _instrumentNotifications.Updated += InstrumentListUpdated;
        _instrumentNotifications.Deleted += InstrumentListUpdated;
    }

    /// <summary>
    /// The default value for the quick filter that includes instrument serial numbers.
    /// </summary>
    public string? DefaultFilterValueSerial { get; private set; }

    /// <summary>
    /// Data source for quick filter that includes instrument serial numbers.
    /// </summary>
    public Dictionary<string, string> FilterDataSourceSerial { get; private set; } = [];

    /// <summary>
    /// The default value for the quick filter that only includes instrument display names.
    /// </summary>
    public string? DefaultFilterValueDisplayName { get; private set; }

    /// <summary>
    /// Data source for quick filter that only includes instrument display names.
    /// </summary>
    public Dictionary<string, string> FilterDataSourceDisplayName { get; private set; } = [];

    public string GetInstrumentName(string serialNumber)
    {
        return FilterDataSourceSerial.GetValueOrDefault(serialNumber, serialNumber);
    }

    public async Task LoadAsync()
    {
        // Shared view model: serialize concurrent loads from multiple pages.
        await _loadLock.WaitAsync();

        try
        {
            if (!NeedsReload)
            {
                return;
            }

            IsBusy = true;
            await DoLoadAsync();
            NeedsReload = false;
        }
        finally
        {
            IsBusy = false;
            _loadLock.Release();
        }
    }

    private async Task DoLoadAsync()
    {
        if (_clientDevice.ClientDeviceType == ClientDeviceType.Instrument)
        {
            var currentInstrumentClient = new CurrentInstrumentClient(_httpClient);
            var currentInstrument = await currentInstrumentClient.GetInstrumentAsync();
            DefaultFilterValueSerial = currentInstrument?.SerialNumber;
            DefaultFilterValueDisplayName = currentInstrument?.DisplayName;
        }
        else
        {
            DefaultFilterValueSerial = null;
        }

        var client = new InstrumentsClient(_httpClient);
        var result = await client.GetAllAsync();

        FilterDataSourceSerial = result.ToDictionary(item => item.SerialNumber, item => item.DisplayName);

        FilterDataSourceDisplayName = result.ToDictionary(item => item.DisplayName, item => item.DisplayName);
    }

    private Task InstrumentListUpdated(int id)
    {
        NeedsReload = true;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _instrumentNotifications.Created -= InstrumentListUpdated;
        _instrumentNotifications.Updated -= InstrumentListUpdated;
        _instrumentNotifications.Deleted -= InstrumentListUpdated;
        _loadLock.Dispose();
    }
}


