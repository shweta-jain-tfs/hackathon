using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.Common.App.Components;

/// <summary>
/// Reports UI activity and displays the service's automatic logout warning.
/// </summary>
public partial class AutoLogoutMonitor : ComponentBase, IDisposable
{
    [Inject] private IAutoLogoutService AutoLogoutService { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;

    [Parameter] public RenderFragment ChildContent { get; set; } = null!;

    private IModalReference? _warningModal;
    private DateTimeOffset? _logoutAt;
    private bool _disposed;

    /// <summary>
    /// Subscribes to warning changes from the service.
    /// </summary>
    protected override void OnInitialized()
    {
        AutoLogoutService.UserIdle += OnUserIdle;
        AutoLogoutService.UserActive += OnUserActive;
    }

    /// <summary>
    /// Starts monitoring after the first render.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AutoLogoutService.StartAsync();
        }
    }

    /// <summary>
    /// Shows the warning with its countdown deadline.
    /// </summary>
    private Task OnUserIdle(int secondsRemaining)
    {
        return UpdateWarningAsync(DateTimeOffset.UtcNow.AddSeconds(secondsRemaining));
    }

    /// <summary>
    /// Dismisses the warning when the service clears it.
    /// </summary>
    private Task OnUserActive() => UpdateWarningAsync(null);

    /// <summary>
    /// Updates the warning on the UI context, ignoring outdated updates.
    /// </summary>
    private Task UpdateWarningAsync(DateTimeOffset? logoutAt)
    {
        _logoutAt = logoutAt;
        return InvokeAsync(() =>
        {
            // Ignore a queued update if another logout deadline has already arrived.
            if (_disposed || _logoutAt != logoutAt)
            {
                return;
            }

            CloseWarningModal();
            if (logoutAt is null || logoutAt <= DateTimeOffset.UtcNow)
            {
                return;
            }

            var parameters = new ModalParameters
            {
                { nameof(AutoLogoutWarningModal.LogoutAt), logoutAt.Value }
            };
            var options = new ModalOptions
            {
                Title = Loc["AutoLogoutWarningModal_Title"],
                ModalHeight = ModalSize.XSmall,
                ModalWidth = ModalSize.Small,
                Status = ModalStatus.Info,
                ShowCloseButton = false
            };
            _warningModal = Modal.Show<AutoLogoutWarningModal>(options, parameters);
        });
    }

    /// <summary>
    /// Reports screen activity to restart the timeout.
    /// </summary>
    private void OnUserActivity() => AutoLogoutService.StayLoggedIn();

    /// <summary>
    /// Closes the warning without affecting other dialogs.
    /// </summary>
    private void CloseWarningModal()
    {
        if (_warningModal is null)
        {
            return;
        }

        Modal.CloseModal(_warningModal);
        _warningModal = null;
    }

    /// <summary>
    /// Unsubscribes from the service and closes the warning.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        AutoLogoutService.UserIdle -= OnUserIdle;
        AutoLogoutService.UserActive -= OnUserActive;
        CloseWarningModal();
        GC.SuppressFinalize(this);
    }
}
