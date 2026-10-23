using Pharos.Client.App.LoadList.Models;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.LoadList.ViewModels;

public class LoadListPageViewModel : LoadingViewModel, IDisposable
{
    private readonly ILoadListDataSource _dataSource;
    private readonly ILoadListNotificationProvider _notificationProvider;

    public LoadListPageViewModel(ILoadListDataSource dataSource, ILoadListNotificationProvider notificationProvider)
    {
        _dataSource = dataSource;
        _notificationProvider = notificationProvider;
        _notificationProvider.BottleLoadListUpdated += OnBottleLoadListUpdated;
        _notificationProvider.CarrierLoadListUpdated += OnCarrierLoadListUpdated;
        _notificationProvider.CartridgeLoadListUpdated += OnCartridgeLoadListUpdated;
        _notificationProvider.DilutionPlateLoadListUpdated += OnDilutionPlateLoadListUpdated;
        _notificationProvider.SampleTubeLoadListUpdated += OnSampleTubeLoadListUpdated;
    }

    public ICollection<LoadListBottle>? BottleLoadList { get; private set; }
    public ICollection<LoadListCarrier>? CarrierLoadList { get; private set; }
    public ICollection<LoadListCartridge>? CartridgeLoadList { get; private set; }
    public ICollection<LoadListDilutionPlate>? DilutionPlateLoadList { get; private set; }
    public ICollection<LoadListSampleTube>? SampleTubeLoadList { get; private set; }

    private async void OnBottleLoadListUpdated(object? sender, EventArgs e)
    {
        BottleLoadList = await _dataSource.GetBottlesAsync();
        OnPropertyChanged(null);
    }

    private async void OnCarrierLoadListUpdated(object? sender, EventArgs e)
    {
        CarrierLoadList = await _dataSource.GetCarriersAsync();
        OnPropertyChanged("ReagentLoaders");
    }
    private async void OnCartridgeLoadListUpdated(object? sender, EventArgs e)
    {
        CartridgeLoadList = await _dataSource.GetCartridgesAsync();
        OnPropertyChanged("ReagentLoaders");
    }
    private async void OnSampleTubeLoadListUpdated(object? sender, EventArgs e)
    {
        SampleTubeLoadList = await _dataSource.GetSampleTubesAsync();
        OnPropertyChanged(null);
    }

    private async void OnDilutionPlateLoadListUpdated(object? sender, EventArgs e)
    {
        DilutionPlateLoadList = await _dataSource.GetDilutionPlatesAsync();
        OnPropertyChanged(null);
    }

    protected override async Task DoLoadAsync()
    {
        var bottlesTask = _dataSource.GetBottlesAsync();
        var carriersTask = _dataSource.GetCarriersAsync();
        var cartridgesTask = _dataSource.GetCartridgesAsync();
        var dilutionPlatesTask = _dataSource.GetDilutionPlatesAsync();
        var sampleTubesTask = _dataSource.GetSampleTubesAsync();

        await Task.WhenAll(bottlesTask, carriersTask, cartridgesTask, dilutionPlatesTask, sampleTubesTask);

        BottleLoadList = bottlesTask.Result;
        CarrierLoadList = carriersTask.Result;
        CartridgeLoadList = cartridgesTask.Result;
        DilutionPlateLoadList = dilutionPlatesTask.Result;
        SampleTubeLoadList = sampleTubesTask.Result;
    }

    public void Dispose()
    {
        _notificationProvider.BottleLoadListUpdated -= OnBottleLoadListUpdated;
        _notificationProvider.CarrierLoadListUpdated -= OnCarrierLoadListUpdated;
        _notificationProvider.CartridgeLoadListUpdated -= OnCartridgeLoadListUpdated;
        _notificationProvider.DilutionPlateLoadListUpdated -= OnDilutionPlateLoadListUpdated;
        _notificationProvider.SampleTubeLoadListUpdated -= OnSampleTubeLoadListUpdated;
        GC.SuppressFinalize(this);
    }

    public async Task<int> GetReagentLoadersCount(IEnumerable<string>? instrumentNumbers, IEnumerable<LoadListStatus>? status)
    {
        return await _dataSource.GetReagentLoadersCountAsync(instrumentNumbers, status);
    }
}
