using Pharos.Client.Common.App.Extensions;
using Pharos.Client.Common.App.Layout.Desktop.Temp;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Contexts;

namespace Pharos.Client.Common.App.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private IConnectionHealthService ConnectionHealthService { get; set; } = null!;
    [Inject] private NavigationHistoryManager? HistoryManager { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private LoaderService LoaderService { get; set; } = null!;
    [Inject] private ModalState ModalState { get; set; } = default!;



    [CascadingParameter(Name = "UiContext")] public UiContext UiContext { get; set; } = null!;

    public DeviceType Device => UiContext.Device;

    private static string InstrumentAppLayoutCssClasses =>
#if DEBUG_1080P
        "ph-app-layout ph-app-layout--debug-1080p ph-theme-instrument";
#else
        "ph-app-layout ph-theme-instrument";
#endif

    private IModalReference? _connectionModalReference;
    private ErrorBoundary? _errorBoundaryBody;
    private ErrorBoundary? _errorBoundaryModal;
    private ErrorBoundary? _errorBoundaryStatusBar;
    private ErrorBoundary? _errorBoundaryNavBar;
    private bool _suppressErrors;
    private CancellationTokenSource? _startCts;


    protected override void OnInitialized()
    {
        // Listen for connection events.
        ConnectionHealthService.ConnectionLost += OnConnectionLost;
        ConnectionHealthService.ConnectionRestored += CloseConnectionModal;
        NavigationManager.LocationChanged += OnLocationChanged;
        TempInstrumentAccessModal();
    }

    protected override void OnParametersSet()
    {
        _errorBoundaryBody?.Recover();
        _errorBoundaryModal?.Recover();
        _errorBoundaryStatusBar?.Recover();
        _errorBoundaryNavBar?.Recover();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Create a CTS to allow cancelling the startup.
            _startCts = new CancellationTokenSource();
            var connectionHealthStartup = ConnectionHealthService.StartAsync(_startCts.Token);

            try
            {
                await connectionHealthStartup;
            }
            catch (OperationCanceledException)
            {
                // Startup was cancelled; ignore.
            }
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // Hide loader (spinner) when navigation away.
        LoaderService.HideVisual();
    }

    private Task OnConnectionLost()
    {
#if !DEBUG
        // Only show connection lost modal in release builds (not in debug mode)
        _connectionModalReference = Modal.Show<Components.ConnectionLostModal>(new ModalOptions { Title = Loc["ConnectionLostModal_Title"], ShowCloseButton = false });
#endif
        return Task.CompletedTask;
    }

    private void CloseModalError()
    {
        ModalState.CancelModals();
        _errorBoundaryModal?.Recover();
    }

    private void ReloadModalError()
    {
        ModalState.CancelModals();
        _errorBoundaryModal?.Recover();
        Reload(false);
    }

    private Task CloseConnectionModal()
    {
        if (_connectionModalReference == null)
        {
            return Task.CompletedTask;
        }

        // Close the modal.
        Modal.CloseModal(_connectionModalReference);
        _connectionModalReference = null;
        return Task.CompletedTask;
    }

    private void Close()
    {
        HistoryManager?.NavigateBack();
    }

    private void Reload(bool suppress)
    {
        _suppressErrors = suppress;
        HistoryManager?.ForceReload();
    }

    public void Dispose()
    {
        // Cancel any pending startup work.
        _startCts?.Cancel();
        _startCts?.Dispose();
        ConnectionHealthService.ConnectionLost -= OnConnectionLost;
        ConnectionHealthService.ConnectionRestored -= CloseConnectionModal;
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }

    private void TempInstrumentAccessModal()
    {
        if (Device == DeviceType.Instrument)
        {
            return;
        }

        Modal.Show<DesktopInstrumentInformationModal>(
            new ModalOptions
            {
                Title = "How to access the instrument screen",
                ModalHeight = ModalSize.XLarge,
                ModalWidth = ModalSize.XLarge,
                ShowCloseButton = false
            }
            );
    }
}
