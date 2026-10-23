using Pharos.Client.App.Reagents.Models.BottleDrawer;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;
using System.ComponentModel;
using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.App.Reagents.ViewModels.BottleDrawer;

public class BottleDrawerViewModel : LoadingViewModel, IDisposable
{
    private Timer? _flashingTimer;
    private readonly IBottleDrawerDataSource _bottleDrawerDataSource;
    protected ICurrentInstrumentNotificationProvider NotificationProvider { get; }
    public BottleDrawerPanelViewModel BottlePanelViewModel { get; set; }
    public BottleLegendViewModel BottleLegendViewModel { get; set; }
    public BottleStatus? SelectedLegendStatus => BottleLegendViewModel.SelectedLegendStatus;
    public bool IsDrawerDim => SelectedLegendStatus.HasValue;

    public event Action? OnDrawerClosed;

    public BottleDrawerViewModel(ICurrentInstrumentNotificationProvider notificationProvider, IBottleDrawerDataSource bottleDrawerDataSource, BottleDrawerPanelViewModel bottlePanelViewModel)
    {
        NotificationProvider = notificationProvider;
        _bottleDrawerDataSource = bottleDrawerDataSource;
        BottlePanelViewModel = bottlePanelViewModel;
        BottleLegendViewModel = new BottleLegendViewModel();
        BottleLegendViewModel.PropertyChanged += OnBottleLegendViewModelPropertyChanged;
    }

    public List<BottlePosition> BottlePositions
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(BottlePositions));
            UpdateBottlePanel();
            UpdateLegend();
        }
    } = new();

    public bool IsDoorOpen
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsDoorOpen));
        }
    }

    public int? FlashingBottleId
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(FlashingBottleId));
            }
        }
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        StopFlashing();
        BottleLegendViewModel.PropertyChanged -= OnBottleLegendViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }

    protected override Task DoLoadAsync()
    {
        SubscribeToNotifications();
        return Task.CompletedTask;
    }

    protected void SubscribeToNotifications()
    {
        NotificationProvider.BottleDrawerContentsChanged += OnDrawerContentsChanged;
        NotificationProvider.BottleDrawerLockStateChanged += OnDrawerLockStateChanged;
    }

    protected void UnsubscribeFromNotifications()
    {
        NotificationProvider.BottleDrawerContentsChanged -= OnDrawerContentsChanged;
        NotificationProvider.BottleDrawerLockStateChanged -= OnDrawerLockStateChanged;
    }

    internal void UpdateDrawerDetails(BottleDrawerDetails bottleDrawerDetails)
    {
        BottlePositions = bottleDrawerDetails.Positions.ToList();
        IsDoorOpen = CheckDoorOpen(bottleDrawerDetails.LockState);
    }

    public async Task OnDrawerContentsChanged()
    {
        var bottleDrawer = await _bottleDrawerDataSource.GetBottleDrawerDetailsAsync();
        UpdateDrawerDetails(bottleDrawer);
    }

    public void OnDrawerLockStateChanged(LockState lockState)
    {
        IsDoorOpen = CheckDoorOpen(lockState);

        if (!IsDoorOpen && IsLoading())
        {
            OnDrawerClosed?.Invoke();
        }
    }

    private static bool CheckDoorOpen(LockState lockState)
    {
        return lockState == LockState.Open;
    }

    private void UpdateBottlePanel()
    {
        if(IsSelectedPositionEmpty())
        {
            BottlePanelViewModel.ClosePanel();
        }
        else if (BottlePanelViewModel != null && BottlePanelViewModel.SelectedBottle != null)
        {
            UpdatePanelInformation(BottlePositions, BottlePanelViewModel.SelectedBottle?.Id);
        }
    }

    public bool UpdatePanelInformation(List<BottlePosition> bottlePositions, int? selectedBottleId)
    {
        var result = (
            from bp in bottlePositions
            from bd in bp.Bottles
            where bd.Id == selectedBottleId
            select new { BottlePosition = bp, BottleDetail = bd }
        ).FirstOrDefault();

        if (result != null)
        {
            var bottleDetail = result.BottleDetail;
            var bottlePosition = result.BottlePosition;

            BottlePanelViewModel.SelectedBottle = bottleDetail;
            BottlePanelViewModel.Type = bottlePosition.ArticleType;
            BottlePanelViewModel.Size = bottlePosition.Size;
            BottlePanelViewModel.Colors = bottlePosition.Colors;
            return true;
        }

        return false;
    }

    public bool IsSelectedPositionEmpty()
    {
        return !BottlePositions.Any(bp => bp.Bottles.Any(bd => bd.Id == BottlePanelViewModel.SelectedBottle?.Id && bd.IsPresent));
    }
    public int? GetSelectedBottleId()
    {
        return BottlePanelViewModel.IsLoading ? null : BottlePanelViewModel.SelectedBottle?.Id;
    }

    public bool IsLoading()
    {
        return BottlePanelViewModel.IsLoading;
    }

    public void StartLoadingSequence(LoadingPosition loadingPosition)
    {
        BottlePanelViewModel.StartLoadingSequence(loadingPosition);
        StartFlashing(loadingPosition.Id);
    }

    public void StopLoadingSequence()
    {
        BottlePanelViewModel.StopLoadingSequence();
        StopFlashing();
    }

    public void BottleSelectedWhileLoading()
    {
        BottlePanelViewModel.BottleSelectedWhileLoading();
        StopFlashing();
    }

    public void ClearLegendSelection()
    {
        BottleLegendViewModel.ClearLegendSelection();
    }

    private void StartFlashing(int bottleId)
    {
        // Stop any existing timer
        StopFlashing();
        
        // Set the flashing bottle ID
        FlashingBottleId = bottleId;
        
        // Start a 15-second timer
        _flashingTimer = new Timer(OnFlashingTimerElapsed, null, TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);
    }

    private void OnFlashingTimerElapsed(object? state)
    {
        StopLoadingSequence();
    }

    private void StopFlashing()
    {
        _flashingTimer?.Dispose();
        _flashingTimer = null;
        FlashingBottleId = null;
    }

    private void UpdateLegend()
    {
        BottleLegendViewModel.UpdateLegend(BottlePositions);
    }

    private void OnBottleLegendViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BottleLegendViewModel.SelectedLegendStatus))
        {
            OnPropertyChanged(nameof(SelectedLegendStatus));
            OnPropertyChanged(nameof(IsDrawerDim));
        }
    }

    public void RestartLoadingTimer()
    {
        if (_flashingTimer != null && FlashingBottleId.HasValue)
        {
            // Dispose existing timer
            _flashingTimer.Dispose();
            
            // Start a new 15-second timer
            _flashingTimer = new Timer(OnFlashingTimerElapsed, null, TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);
        }
    }
}
