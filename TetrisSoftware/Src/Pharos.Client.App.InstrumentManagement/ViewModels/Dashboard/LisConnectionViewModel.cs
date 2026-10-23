using Pharos.Client.App.InstrumentManagement.Models.Dashboard;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;

public sealed class LisConnectionViewModel : LoadingViewModel, IDisposable
{
    private readonly ILisConnectionDataService _dataService;
    private readonly ILisNotificationProvider _notificationProvider;

    public LisConnectionViewModel(
        ILisConnectionDataService dataService,
        ILisNotificationProvider notificationProvider)
    {
        _dataService = dataService;
        _notificationProvider = notificationProvider;
        _notificationProvider.LisStatusChanged += OnLisStatusChanged;
        _notificationProvider.LisStatisticsChanged += OnLisStatisticsChanged;
    }

    public List<LisConnectionSummary> Connections { get; private set; } = [];

    public void Dispose()
    {
        _notificationProvider.LisStatusChanged -= OnLisStatusChanged;
        _notificationProvider.LisStatisticsChanged -= OnLisStatisticsChanged;
        GC.SuppressFinalize(this);
    }

    protected override async Task DoLoadAsync()
    {
        Connections = [.. await _dataService.GetAllSummaryAsync()];
    }

    public async Task ResetStatisticsAsync(int channelId)
    {
        await _dataService.ResetStatisticsAsync(channelId);
    }

    private async void OnLisStatusChanged(object? sender, EventArgs e)
    {
        Connections = [.. await _dataService.GetAllSummaryAsync()];
        OnPropertyChanged(null);
    }

    private async void OnLisStatisticsChanged(int channelId)
    {
        var summary = await _dataService.GetSummaryByChannelIdAsync(channelId);

        var channelSummary = Connections.FirstOrDefault(c => c.Id == channelId);
        if (channelSummary == null)
        {
            return;
        }
        var index = Connections.IndexOf(channelSummary);
        Connections[index] = summary;
        OnPropertyChanged(null);
    }
}