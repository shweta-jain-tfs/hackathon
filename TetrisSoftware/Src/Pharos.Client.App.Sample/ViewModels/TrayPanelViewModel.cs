using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Drawer;
using SampleType = Pharos.Common.Web.Components.Drawer.SampleType;

namespace Pharos.Client.App.Sample.ViewModels;

public class TrayPanelViewModel : EntityEditorViewModel<SampleTrayDetails, string>
{
    private readonly ICurrentInstrumentNotificationProvider _instrumentNotificationProvider;
    private readonly ISampleTrayPositionNotificationProvider _sampleTrayPositionNotificationProvider;
    private readonly ISampleTrayDetailsDataService _sampleTrayDetailsDataService;
    private readonly IRequestDataSource _requestDataSource;
    private readonly IExceptionHandlerService _exceptionHandler;

    private SampleTube? _selectedSampleTube;
    private string _tubeBarcode = string.Empty;
    private bool _highPriority;
    private string _sampleTubeStatus = string.Empty;
    private bool _hasLegendView;
    private SampleType _selectedSampleType;
    private bool _isLeftDrawerScanning;
    private bool _isRightDrawerScanning;
    private bool _needsReloadOnPositionStatusUpdate;

    private bool IsScanningOngoing => _isLeftDrawerScanning || _isRightDrawerScanning;

    public bool IsSampleTubeSelected { get; set; }

    public bool IsSelectionChanged { get; set; } = true;

    public SampleLegendViewModel LegendViewModel { get; }

    public string TrayId => Entity.Id;

    public string TrayLocation => Entity.TrayLocation;

    public bool IsHighPriorityCheckBoxVisible => _selectedSampleTube?.SampleType != SampleType.QualityControl;

    public string SampleTubeStatus
    {
        get => _sampleTubeStatus;
        set
        {
            _sampleTubeStatus = value;
            OnPropertyChanged(nameof(SampleTubeStatus));
        }
    }

    public string SampleIdDisplay { get; private set; } = string.Empty;

    public string TubeBarcode
    {
        get => _tubeBarcode;
        set
        {
            _tubeBarcode = value;
            IsModified = true;
            OnPropertyChanged(nameof(TubeBarcode));
        }
    }
    public bool HighPriority
    {
        get => _highPriority;
        set
        {
            _highPriority = value;
            IsModified = true;
            OnPropertyChanged(nameof(HighPriority));
        }
    }

    public bool HasLegendView
    {
        get => _hasLegendView;
        set
        {
            _hasLegendView = value;
            OnPropertyChanged(nameof(HasLegendView));
        }
    }

    public bool IsTrayEditable
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsTrayEditable));
        }
    }

    public bool IsEditMode
    {
        get;
        set
        {
            field = value;
            SetEnableHighPriorityFlag();
            OnPropertyChanged(nameof(IsEditMode));
        }
    }

    public bool EnableHighPriority
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(EnableHighPriority));
        }
    }

    public bool IsRequestAvailable
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsRequestAvailable));
        }
    }

    public int RequestId
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(RequestId));
        }
    }

    public SampleType SelectedSampleType
    {
        get => _selectedSampleType;
        set
        {
            _selectedSampleType = value;
            OnPropertyChanged(nameof(SelectedSampleType));
        }
    }

    public TrayPanelViewModel(ISampleTrayDetailsDataService sampleTrayDetailsDataService,
        ISampleTrayPositionNotificationProvider sampleTrayPositionNotificationProvider,
        ICurrentInstrumentNotificationProvider instrumentNotificationProvider,
        SampleLegendViewModel legendViewModel,
        IRequestDataSource requestDataSource,
        IExceptionHandlerService exceptionHandler)
        : base(sampleTrayDetailsDataService)
    {
        _sampleTrayPositionNotificationProvider = sampleTrayPositionNotificationProvider;
        _instrumentNotificationProvider = instrumentNotificationProvider;
        _sampleTrayDetailsDataService = sampleTrayDetailsDataService;
        _requestDataSource = requestDataSource;
        LegendViewModel = legendViewModel;
        _exceptionHandler = exceptionHandler;
        SubscribeToNotifications();
    }

    public event EventHandler? DrawerContentsReceived;

    public List<SampleTube> GetSampleTubes()
    {
        return Entity.SamplePositions?.ToSampleTubes() ?? [];
    }

    /// <summary>
    /// Assign respective values of fields in Tray Panel when sample tube is selected.
    /// </summary>
    /// <param name="sampleTube">Selected sample tube.</param>
    public async Task SampleTubeSelected(SampleTube sampleTube)
    {
        _selectedSampleTube = sampleTube;
        await RequestForSampleIdAsync();

        IsModified = false;
        IsSampleTubeSelected = true;
        IsSelectionChanged = true;
        SampleIdDisplay = sampleTube.SampleIdDisplay;
        _tubeBarcode = sampleTube.TubeBarcode;
        _highPriority = sampleTube.IsHighPriority;
        _selectedSampleType = sampleTube.SampleType;
        IsTrayEditable = Entity.IsTrayEditable;

        SetSelectedSampleTubeStatus(sampleTube);
        SetEnableHighPriorityFlag();
    }

    public void EditTrayPanelButtonClicked()
    {
        IsEditMode = true;
    }

    public async Task DiscardChangesAsync()
    {
        await InitializeAsync();
    }

    public async Task RequestForSampleIdAsync()
    {
        var sampleId = _selectedSampleTube?.SampleId;

        if (string.IsNullOrEmpty(sampleId) || _selectedSampleTube?.SampleType is not SampleType.Sample)
        {
            RequestId = -1;
            IsRequestAvailable = false;
            return;
        }

        var requestDetails = await _requestDataSource.GetRequestIdAsync(sampleId);
        RequestId = requestDetails.RequestId;
        IsRequestAvailable = requestDetails.RequestExists;
    }

    public override async Task<bool> SaveAsync()
    {
        if (_selectedSampleTube?.Position is null)
        {
            return false;
        }

        if (!IsModified && _selectedSampleTube.PositionSource is TubePositionSource.ConfirmManualInput)
        {
            IsModified = true;
        }

        if (!IsModified)
        {
            return false;
        }

        _needsReloadOnPositionStatusUpdate = true;

        try
        {
            var position = GetSelectedSampleTubePosition();
            await _sampleTrayDetailsDataService.UpdateSamplePositionAsync(Entity.Id, position.Position!.Value, _tubeBarcode, _highPriority);

            IsModified = false;
        }
        catch (ApiException ex)
        {
            _needsReloadOnPositionStatusUpdate = false;
            _exceptionHandler.ShowExceptionMessage(ex);
            return false;
        }
        finally
        {
            NeedsReload = true;
        }

        return true;
    }

    public void SetLegendVisibility(bool isVisible)
    {
        _hasLegendView = isVisible;
    }

    protected override async Task DoLoadAsync(string entityId)
    {
        await base.DoLoadAsync(entityId);

        await InitializeAsync();

        if (_hasLegendView)
        {
            LoadLegendViewModel();
        }
    }

    protected override void UnsubscribeFromNotifications()
    {
        _instrumentNotificationProvider.SampleDrawerContentsReceived -= OnSampleDrawerContentsReceived;
        _instrumentNotificationProvider.LeftSampleDrawerScanningStatusChanged -= OnLeftDrawerScanningStatusChanged;
        _instrumentNotificationProvider.RightSampleDrawerScanningStatusChanged -= OnRightDrawerScanningStatusChanged;
        _sampleTrayPositionNotificationProvider.PositionStatusUpdated -= OnSampleTrayPositionStatusUpdated;

        base.UnsubscribeFromNotifications();
    }

    private void SubscribeToNotifications()
    {
        _instrumentNotificationProvider.SampleDrawerContentsReceived += OnSampleDrawerContentsReceived;
        _instrumentNotificationProvider.LeftSampleDrawerScanningStatusChanged += OnLeftDrawerScanningStatusChanged;
        _instrumentNotificationProvider.RightSampleDrawerScanningStatusChanged += OnRightDrawerScanningStatusChanged;
        _sampleTrayPositionNotificationProvider.PositionStatusUpdated += OnSampleTrayPositionStatusUpdated;
    }

    private void OnSampleDrawerContentsReceived(object? sender, List<string> trayIds)
    {
        if (trayIds.Contains(Entity.Id))
        {
            Entity.IsTrayEditable = false;
            IsTrayEditable = false;
            if (IsEditMode)
            {
                DrawerContentsReceived?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private async Task OnSampleTrayPositionStatusUpdated(List<string> trayIds)
    {
        if (!trayIds.Contains(Entity.Id))
        {
            return;
        }

        if (IsScanningOngoing && !_needsReloadOnPositionStatusUpdate)
        {
            return;
        }

        _needsReloadOnPositionStatusUpdate = false;

        await DoLoadAsync(Entity.Id);
        OnPropertyChanged(nameof(IsModified));
    }

    private void OnLeftDrawerScanningStatusChanged(bool isScanning) => _isLeftDrawerScanning = isScanning;

    private void OnRightDrawerScanningStatusChanged(bool isScanning) => _isRightDrawerScanning = isScanning;

    private void LoadLegendViewModel()
    {
        LegendViewModel?.UpdateLegend([Entity]);
    }

    private SampleTrayPosition GetSelectedSampleTubePosition()
    {
        return Entity.SamplePositions.First(x => x.Position == _selectedSampleTube!.Position);
    }

    private async Task InitializeAsync()
    {
        if (IsSampleTubeSelected)
        {
            var position = GetSelectedSampleTubePosition();
            var sampleTube = position.ToSampleTube();
            await SampleTubeSelected(sampleTube);
        }
        else
        {
            _selectedSampleTube = default!;
            SampleIdDisplay = string.Empty;
            _tubeBarcode = string.Empty;
            _highPriority = false;
            _sampleTubeStatus = string.Empty;
            IsTrayEditable = false;
        }

        IsEditMode = false;
    }

    private void SetEnableHighPriorityFlag()
    {
        EnableHighPriority = _selectedSampleTube?.TubeError is TubeError.None
                               && IsEditMode;
    }

    /// <summary>
    /// Sets the display value for "Status" field in the Tray Panel. 
    /// </summary>
    /// <param name="selectedSampleTube">Selected sample tube.</param>
    private void SetSelectedSampleTubeStatus(SampleTube selectedSampleTube)
    {
        if (selectedSampleTube.TubeError is TubeError.None)
        {
            _sampleTubeStatus = selectedSampleTube.Status is TubeStatus.Empty
               ? string.Empty
               : selectedSampleTube.Status.ToLocalizedString("TrayPanel");
        }
        else
        {
            _sampleTubeStatus = GetSelectedSampleTubePosition().Error.ToLocalizedString("TrayPanel");
        }
    }
}
