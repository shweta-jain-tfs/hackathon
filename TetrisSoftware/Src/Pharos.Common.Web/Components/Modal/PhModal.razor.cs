using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Pharos.Common.Web.Extensions;

namespace Pharos.Common.Web.Components.Modal;

/// <summary>
/// Container component for rendering modal dialogs (<see cref="ModalInstance"/>).
/// Manages the display and lifecycle of all modals in the application.
/// </summary>
public partial class PhModal
{
    /// <summary>
    /// Gets or sets the state of the modal.
    /// </summary>
    [Inject] public ModalState ModalState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager used to handle location changes.
    /// </summary>
    [Inject] public NavigationManager NavManager { get; set; } = default!;

    private IJSObjectReference? _script;

    /// <summary>
    /// Initializes the modal component.
    /// </summary>
    protected override void OnInitialized()
    {
        NavManager.LocationChanged += CancelModals;
        ModalState.OnModalInstanceAdded += AddModal;
        ModalState.OnModalCloseRequested += CloseModal;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // pre-load modal script to have it available for "lost connection" modal
            _script = await ImportCommonUtilityJSAsync("modal.js");
        }
    }

    /// <summary>
    /// Closes the modal and removes the code from the html.
    /// </summary>
    private void CloseModal()
    {
        StateHasChanged();
    }

    /// <summary>
    /// Adds the new modal with the specified reference in html code.
    /// </summary>
    private void AddModal()
    {
        StateHasChanged();
    }

    /// <summary>
    /// Cancels all modals when the location changes.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void CancelModals(object? sender, LocationChangedEventArgs e)
    {
        var modals = ModalState._modals;
        var modalsToKeep = modals.Where(m => !m.CloseModalOnNavigation).ToList();

        if (modalsToKeep is { Count: > 0 })
        {
            var modalsToClose = modals.Where(m => m.CloseModalOnNavigation).ToList();
            foreach (var modalReference in modalsToClose)
            {
                ModalState.RemoveModal(modalReference);
            }
        }
        else
        {
            ModalState.CancelModals();
        }

        StateHasChanged();
    }

    /// <summary>
    /// Disposes the modal component.
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        NavManager.LocationChanged -= CancelModals;
        ModalState.OnModalInstanceAdded -= AddModal;
        ModalState.OnModalCloseRequested -= CloseModal;

        await _script.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }
}
