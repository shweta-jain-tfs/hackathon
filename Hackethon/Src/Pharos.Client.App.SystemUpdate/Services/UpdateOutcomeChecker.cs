using Microsoft.Extensions.Localization;
using Pharos.Client.App.SystemUpdate.Models;
using Pharos.Client.App.SystemUpdate.Notifications;
using Pharos.Client.App.SystemUpdate.Views;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models.Services;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.SystemUpdate.Services;

public sealed class UpdateOutcomeChecker : IAsyncDisposable
{
    private readonly ISystemUpdateDataService _dataService;
    private readonly ISystemUpdateNotificationProvider _notificationProvider;
    private readonly IModalService _modalService;
    private readonly IConnectionHealthService _connectionHealthService;
    private readonly IAppNavigationService _navigation;
    private readonly IStringLocalizer<Translations> _loc;

    private IModalReference? _modal;
    private IModalReference? _inProgressModal;
    private bool _initialized;
    private bool _reloadAfterAcknowledge;

    public UpdateOutcomeChecker(ISystemUpdateDataService dataService,
        ISystemUpdateNotificationProvider notificationProvider,
        IModalService modalService,
        IConnectionHealthService connectionHealthService,
        IAppNavigationService navigation,
        IStringLocalizer<Translations> loc)
    {
        _dataService = dataService;
        _notificationProvider = notificationProvider;
        _modalService = modalService;
        _connectionHealthService = connectionHealthService;
        _navigation = navigation;
        _loc = loc;
    }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        _notificationProvider.UpdateInProgress += OnUpdateInProgress;
        _notificationProvider.UpdateFailed += OnUpdateFailed;
        _notificationProvider.UpdateOutcomeAcknowledged += OnUpdateOutcomeAcknowledged;
        _connectionHealthService.ConnectionRestored += OnConnectionRestoredAsync;

        await CheckAndDisplaySoftwareUpdateOutcome();
    }

    private void OnUpdateInProgress(object? sender, EventArgs e)
    {
        if (_inProgressModal is not null)
        {
            return;
        }

        var options = new ModalOptions
        {
            Title = _loc["UpdateInProgressView_Title"],
            ModalWidth = ModalSize.XLarge,
            ModalHeight = ModalSize.XLarge,
            Status = ModalStatus.Info,
            ShowCloseButton = false,
            CloseOnNavigation = false
        };

        _inProgressModal = _modalService.Show<UpdateInProgressView>(options);
    }

    private void OnUpdateFailed(object? sender, EventArgs e)
    {
        ShowUpdateOutcomeAndReloadIfUpdated(success: false);
    }

    private void OnUpdateOutcomeAcknowledged(object? sender, EventArgs e)
    {
        if (_modal is null)
        {
            return;
        }

        _modalService.CloseModal(_modal);
        _modal = null;

        if (_reloadAfterAcknowledge)
        {
            _navigation.Reload();
        }
    }

    private async Task OnConnectionRestoredAsync()
    {
        // Connection might have been lost due to the update on the server, so check if software was updated when connection is restored.
        await CheckAndDisplaySoftwareUpdateOutcome();
    }

    private async Task CheckAndDisplaySoftwareUpdateOutcome()
    {
        var outcome = await _dataService.GetUnacknowledgedOutcome();

        if (!outcome.IsAcknowledged)
        {
            ShowUpdateOutcomeAndReloadIfUpdated(outcome.IsSuccessful);
        }
    }

    private void ShowUpdateOutcomeAndReloadIfUpdated(bool success)
    {
        CloseInProgressModal();

        if (_modal is not null)
        {
            return;
        }

        var options = new ModalOptions
        {
            Title = _loc["UpdateOutcome_Title"],
            ModalWidth = ModalSize.XLarge,
            ModalHeight = ModalSize.XLarge,
            Status = success ? ModalStatus.Info : ModalStatus.Warning,
            ShowCloseButton = false,
            CloseOnNavigation = false
        };

        _modal = _modalService.ShowInformation(
            success ? _loc["UpdateOutcome_Success"] : _loc["UpdateOutcome_Failed"],
            _loc["UpdateOutcome_Close"],
            options,
            true);

        // Reload if update was successful to ensure we show the latest content after an update, but if it failed then there's no need to reload since the software didn't actually update.
        _reloadAfterAcknowledge = success;

        _ = AcknowledgeWhenClosedAsync(_modal);
    }

    private async Task AcknowledgeWhenClosedAsync(IModalReference modalRef)
    {
        await modalRef.Result;
        await _dataService.AcknowledgeUpdate();
    }

    public ValueTask DisposeAsync()
    {
        _notificationProvider.UpdateInProgress -= OnUpdateInProgress;
        _notificationProvider.UpdateFailed -= OnUpdateFailed;
        _notificationProvider.UpdateOutcomeAcknowledged -= OnUpdateOutcomeAcknowledged;
        _connectionHealthService.ConnectionRestored -= OnConnectionRestoredAsync;
        return ValueTask.CompletedTask;
    }

    private void CloseInProgressModal()
    {
        if (_inProgressModal is null)
        {
            return;
        }

        _modalService.CloseModal(_inProgressModal);
        _inProgressModal = null;
    }
}