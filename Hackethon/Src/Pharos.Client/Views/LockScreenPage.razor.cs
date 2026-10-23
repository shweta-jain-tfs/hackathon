using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.App.Components;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models.Services;
using Pharos.Common.Web.Components.Base;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.Views;

public partial class LockScreenPage : ComponentWithClientScriptBase
{
    [Inject] private IConnectionHealthService ConnectionHealthService { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private IModalReference? _connectionModalReference;
    private CancellationTokenSource? _startCts;

    private static string AppLayoutCssClasses =>
#if DEBUG_1080P
        "ph-app-layout ph-app-layout--without-navigation ph-app-layout--debug-1080p ph-theme-instrument";
#else
        "ph-app-layout ph-app-layout--without-navigation ph-theme-instrument";
#endif

    protected override void OnInitialized()
    {
        ConnectionHealthService.ConnectionLost += OnConnectionLost;
        ConnectionHealthService.ConnectionRestored += CloseConnectionModal;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _startCts = new CancellationTokenSource();
            try
            {
                await ConnectionHealthService.StartAsync(_startCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Startup was cancelled; ignore.
            }
        }
    }

    private Task OnConnectionLost()
    {
        _connectionModalReference = Modal.Show<ConnectionLostModal>(new ModalOptions { Title = Loc["ConnectionLostModal_Title"], ShowCloseButton = false });
        return Task.CompletedTask;
    }

    private Task CloseConnectionModal()
    {
        if (_connectionModalReference == null)
        {
            return Task.CompletedTask;
        }

        Modal.CloseModal(_connectionModalReference);
        _connectionModalReference = null;
        return Task.CompletedTask;
    }

    private void HandleClick(MouseEventArgs e)
    {
        Navigation.NavigateTo(AccountRoutes.Login, forceLoad: true);
    }

    public override async ValueTask DisposeAsync()
    {
        _startCts?.Cancel();
        _startCts?.Dispose();

        ConnectionHealthService.ConnectionLost -= OnConnectionLost;
        ConnectionHealthService.ConnectionRestored -= CloseConnectionModal;
        GC.SuppressFinalize(this);
    }
}
