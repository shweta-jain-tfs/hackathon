using Pharos.Client.App.Reagents.Models.BottleDrawer;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;
using Pharos.Contracts.Security;
using ArticleType = Pharos.Client.Common.Models.ArticleType;

namespace Pharos.Client.App.Reagents.ViewModels.BottleDrawer;

public class BottlePageViewModel : LoadingViewModel, IDisposable
{
    private readonly IBottleDrawerDataSource _bottleDrawerDataSource;
    private readonly IAuthorizationService _authorizationService;
    private readonly ITaskCardRefreshNotificationProvider _taskCardRefreshNotificationProvider;
    private readonly ICurrentInstrumentTaskCardItemSource _taskCardItemSource;
    public BottleDrawerViewModel BottleDrawerViewModel { get; set; }
    protected ICurrentInstrumentNotificationProvider NotificationProvider { get; }
    public event Action<ToastType>? OnShowToast;
    private LoadingPosition? _loadingPosition;
    private string? _lastScannedBarcode;

    public enum ToastType
    {
        Unauthorized,
        NoPositionAvailable,
        MismatchedBottleType,
        MultipleScanDetected,
        IncorrectBottlePlacement,
        ScanBottleBeforeLoading,
        IncorrectBarcodeFormat
    }

    public bool IsPanelOpen
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsPanelOpen));
        }
    }

    public int? FlashingBottleId => BottleDrawerViewModel.FlashingBottleId;

    public BottlePageViewModel(BottleDrawerViewModel bottleDrawerViewModel,
                                ICurrentInstrumentNotificationProvider notificationProvider,
                                IBottleDrawerDataSource bottleDrawerDataSource,
                                IAuthorizationService authorizationService,
                                ITaskCardRefreshNotificationProvider taskCardRefreshNotificationProvider,
                                ICurrentInstrumentTaskCardItemSource taskCardItemSource)
    {
        BottleDrawerViewModel = bottleDrawerViewModel;
        _bottleDrawerDataSource = bottleDrawerDataSource;
        NotificationProvider = notificationProvider;
        _authorizationService = authorizationService;
        _taskCardRefreshNotificationProvider = taskCardRefreshNotificationProvider;
        _taskCardItemSource = taskCardItemSource;

        // Subscribe to drawer closed event
        BottleDrawerViewModel.OnDrawerClosed += OnDrawerClosed;
    }

    private void OnDrawerClosed()
    {
        StopLoadingSequence();
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        BottleDrawerViewModel.OnDrawerClosed -= OnDrawerClosed;
        GC.SuppressFinalize(this);
    }

    protected override async Task DoLoadAsync()
    {
        SubscribeToNotifications();
        await BottleDrawerViewModel.LoadAsync();
        var bottleDrawer = await _bottleDrawerDataSource.GetBottleDrawerDetailsAsync();
        BottleDrawerViewModel.UpdateDrawerDetails(bottleDrawer);
        await UpdateTaskCardLegendAsync();
    }

    protected void SubscribeToNotifications()
    {
        NotificationProvider.ReagentBarcodeScanned += OnReagentBarcodeScanned;
        NotificationProvider.BottlePresenceChanged += OnBottlePresenceChanged;
        NotificationProvider.IncorrectBarcodeScannedForBottles += OnIncorrectBarcodeScannedForBottles;
        _taskCardRefreshNotificationProvider.RefreshNeeded += OnTaskCardsRefreshNeeded;
    }

    protected void UnsubscribeFromNotifications()
    {
        NotificationProvider.ReagentBarcodeScanned -= OnReagentBarcodeScanned;
        NotificationProvider.BottlePresenceChanged -= OnBottlePresenceChanged;
        NotificationProvider.IncorrectBarcodeScannedForBottles -= OnIncorrectBarcodeScannedForBottles;
        _taskCardRefreshNotificationProvider.RefreshNeeded -= OnTaskCardsRefreshNeeded;
    }

    private async void OnTaskCardsRefreshNeeded(object? sender, EventArgs e)
    {
        await UpdateTaskCardLegendAsync();
    }

    private async Task UpdateTaskCardLegendAsync()
    {
        var taskCards = await _taskCardItemSource.GetAsync();
        BottleDrawerViewModel.BottleLegendViewModel.UpdateTaskCardLegend(taskCards);
    }

    private async Task OnReagentBarcodeScanned(string barcode)
    {
        var isAuthorized = await _authorizationService.IsAuthorized(Permissions.ManageReagents);
        if (BottleDrawerViewModel.IsDoorOpen)
        {
            if (!isAuthorized)
            {
                OnShowToast?.Invoke(ToastType.Unauthorized);
            }
            else
            {
                if (BottleDrawerViewModel.IsLoading() && _lastScannedBarcode != null)
                {
                    if (_lastScannedBarcode == barcode)
                    {
                        BottleDrawerViewModel.RestartLoadingTimer();
                    }
                    else
                    {
                        StopLoadingSequence();
                        OnShowToast?.Invoke(ToastType.MultipleScanDetected);
                    }
                }
                else
                {
                    // Not currently loading or first scan
                    _loadingPosition = await _bottleDrawerDataSource.GetBottleLoadingPosition(barcode);
                    switch (_loadingPosition.Status)
                    {
                        case LoadingPositionStatus.Found:
                            _lastScannedBarcode = barcode;
                            StartLoadingSequence(_loadingPosition);
                            break;

                        case LoadingPositionStatus.NotFound:
                            _lastScannedBarcode = null;
                            OnShowToast?.Invoke(ToastType.NoPositionAvailable);
                            break;

                        case LoadingPositionStatus.NotValidPair:
                            _lastScannedBarcode = null;
                            OnShowToast?.Invoke(ToastType.MismatchedBottleType);
                            break;
                    }
                }
            }
        }
    }

    private void OnBottlePresenceChanged(int articleType, int position, bool isPresent)
    {
        if (BottleDrawerViewModel.IsLoading())
        {
            if (_loadingPosition != null && isPresent)
            {
                if (_loadingPosition.ArticleType == (ArticleType)articleType && _loadingPosition.Position == position)
                {
                    //Bottle placed in correct position
                    StopLoadingSequence();
                }
                else
                {
                    //Bottle placed in wrong position - abort loading sequence and show toast
                    StopLoadingSequence();
                    OnShowToast?.Invoke(ToastType.IncorrectBottlePlacement);
                }
            }
        }
        else
        {
            if (isPresent)
            {
                // Bottle placed without scanning - show toaster message
                OnShowToast?.Invoke(ToastType.ScanBottleBeforeLoading);
            }
        }
    }

    private void OnIncorrectBarcodeScannedForBottles()
    {
        OnShowToast?.Invoke(ToastType.IncorrectBarcodeFormat);
    }

    private void StartLoadingSequence(LoadingPosition loadingPosition)
    {
        OpenPanel();
        BottleDrawerViewModel.StartLoadingSequence(loadingPosition);
    }

    private void StopLoadingSequence()
    {
        BottleDrawerViewModel.StopLoadingSequence();
        _lastScannedBarcode = null;
        ClosePanel();
    }

    public void OpenPanel()
    {
        IsPanelOpen = true;
    }

    public void ClosePanel()
    {
        IsPanelOpen = false;
    }

    public void CloseClicked()
    {
        if (BottleDrawerViewModel.IsLoading())
        {
            StopLoadingSequence();
        }
        else
        {
            ClosePanel();
        }
    }

    public void ClearLegendSelection()
    {
        BottleDrawerViewModel.ClearLegendSelection();
    }

    public void BottleSelected()
    {
        ClearLegendSelection();

        if (BottleDrawerViewModel.IsLoading())
        {
            BottleSelectedWhileLoading();
        }
        else
        {
            OpenPanel();
        }
    }

    public void BottleSelectedWhileLoading()
    {
        _lastScannedBarcode = null;
        BottleDrawerViewModel.BottleSelectedWhileLoading();
    }
}