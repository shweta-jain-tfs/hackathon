using Pharos.Client.App.InstrumentManagement.Models.Dashboard;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;

public class ConnectionsViewModel : LoadingViewModel, IDisposable
{
    private readonly ILisConnectionDataService _lisDataService;
    private readonly ILisNotificationProvider _lisNotificationProvider;
    private readonly LabCommunityConnectionDataService _labCommunityDataService;
    private readonly ILabCommunityNotificationProvider _labCommunityNotificationProvider;

    public ConnectionsViewModel(ILisConnectionDataService lisDataService, ILisNotificationProvider lisNotificationProvider,
        LabCommunityConnectionDataService labCommunityDataService, ILabCommunityNotificationProvider labCommunityNotificationProvider)
    {
        _lisDataService = lisDataService;
        _lisNotificationProvider = lisNotificationProvider;
        _labCommunityDataService = labCommunityDataService;
        _labCommunityNotificationProvider = labCommunityNotificationProvider;
        _lisNotificationProvider.LisStatusChanged += OnLisStatusChanged;
        _labCommunityNotificationProvider.LabCommunityStatusChanged += OnLabCommunityStatusChanged;
    }

    public StatusType Status { get; set; } = StatusType.Disconnected;
    public bool IsLasConnectionOk { get; set; }
    public bool IsLabCommunityConnectionOk { get; set; }
    public List<LisConnection> LisConnections { get; set; } = new();
    public bool ShowConnectionStatus { get; set; } = true;
    public bool IsLabCommunityEnabled { get; set; }

    // Only called initially.
    protected override async Task DoLoadAsync()
    {
        await Task.WhenAll(LoadLisAsync(), LoadLabCommunityAsync());
        Status = GetOverAllStatus();
    }

    protected async Task LoadLisAsync()
    {
        var lisConnections = await _lisDataService.GetAsync(new DataSourceRequest());
        LisConnections = lisConnections.Items;
        Status = GetOverAllStatus();
    }

    private async void OnLisStatusChanged(object? sender, EventArgs e)
    {
        await LoadLisAsync();
        OnPropertyChanged(null);
    }

    protected async Task LoadLabCommunityAsync()
    {
        var status = await _labCommunityDataService.GetStatusAsync();
        IsLabCommunityConnectionOk = status.IsConnected;
        IsLabCommunityEnabled = await _labCommunityDataService.IsEnabledAsync();
        Status = GetOverAllStatus();
    }

    private async void OnLabCommunityStatusChanged(object? sender, EventArgs e)
    {
        await LoadLabCommunityAsync();
        OnPropertyChanged(null);
    }

    private StatusType GetOverAllStatus()
    {
        // TODO: LAS should be part of the health.
        ShowConnectionStatus = true;
        if (LisConnections.Count == 0 && !IsLabCommunityEnabled)
        {
            ShowConnectionStatus = false;
            return StatusType.None;
        }
        if (LisConnections.All(x => x.State == LisConnectionState.Open))
        {
            if (IsLabCommunityEnabled && !IsLabCommunityConnectionOk)
            {
                return StatusType.Warning;
            }
            return StatusType.Success;
        }

        return StatusType.Alarm;
    }

    public void Dispose()
    {
        _lisNotificationProvider.LisStatusChanged -= OnLisStatusChanged;
        _labCommunityNotificationProvider.LabCommunityStatusChanged -= OnLabCommunityStatusChanged;

        GC.SuppressFinalize(this);
    }
}