using Microsoft.JSInterop;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Modal;

/// <summary>
/// Represents an instance of a modal dialog.
/// </summary>
public partial class ModalInstance
{
    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] public ModalState ModalState { get; set; } = default!;
    [Inject] public NavigationManager NavManager { get; set; } = default!;

    [Inject] public IJSRuntime Js { get; set; } = default!;

    [Parameter] public Guid Id { get; set; }
    [Parameter] public RenderFragment Content { get; set; } = default!;
    [Parameter] public ModalOptions Options { get; set; } = new();

    private ElementReference? _modalElementRef;
    private IJSObjectReference? _script;

    private string? CssClasses => new CssBuilder("ph-modal")
        .AddClass("ph-modal--width-xsmall", Options.ModalWidth == ModalSize.XSmall)
        .AddClass("ph-modal--width-small", Options.ModalWidth == ModalSize.Small)
        .AddClass("ph-modal--width-medium", Options.ModalWidth == ModalSize.Medium)
        .AddClass("ph-modal--width-large", Options.ModalWidth == ModalSize.Large)
        .AddClass("ph-modal--width-xlarge", Options.ModalWidth == ModalSize.XLarge)
        .AddClass("ph-modal--height-xsmall", Options.ModalHeight == ModalSize.XSmall)
        .AddClass("ph-modal--height-small", Options.ModalHeight == ModalSize.Small)
        .AddClass("ph-modal--height-medium", Options.ModalHeight == ModalSize.Medium)
        .AddClass("ph-modal--height-large", Options.ModalHeight == ModalSize.Large)
        .AddClass("ph-modal--height-xlarge", Options.ModalHeight == ModalSize.XLarge)
        .AddClass("ph-modal--info", Options.Status == ModalStatus.Info)
        .AddClass("ph-modal--warning", Options.Status == ModalStatus.Warning)
        .AddClass("ph-modal--alarm", Options.Status == ModalStatus.Alarm)
        .AddClass("ph-modal--centertext", Options.CenterText)
        .Build();

    /// <summary>
    /// Called after the component has been rendered.
    /// - Shows the dialog as a modal if it has a valid ID.
    /// - Updates Escape key behavior based on ShowCloseButton.
    /// </summary>
    /// <param name="firstRender">True if this is the first time the component has been rendered; otherwise, false.</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // This script is pre-loaded in PhModal, but we need a reference here as well to show the modal dialog.
            _script = await ImportCommonUtilityJSAsync("modal.js");

            // Only show the dialog as a modal if it has a valid ID
            if (!string.IsNullOrWhiteSpace(_modalElementRef?.Id))
            {
                await _script.InvokeVoidAsync("showModal", _modalElementRef, !Options.ShowCloseButton);
            }
        }
    }

    /// <summary>
    /// Closes the modal dialog by canceling.
    /// </summary>
    public async Task AbortAsync()
    {
        await CloseAsync(ModalResult.Cancel());
    }

    /// <summary>
    /// Closes the modal dialog with the specified result.
    /// </summary>
    /// <param name="result">The result to return when closing the modal dialog.</param>
    public async Task CloseAsync(ModalResult result)
    {
        await DismissInstance(Id, result);
    }

    /// <summary>
    /// Dismisses the modal instance with the specified result.
    /// </summary>
    /// <param name="id">The identifier of the modal instance.</param>
    /// <param name="result">The result to return when dismissing the modal instance.</param>
    internal Task DismissInstance(Guid id, ModalResult result)
    {
        var reference = GetModalReference(id);
        return DismissInstance(reference, result);
    }

    /// <summary>
    /// Dismisses the modal instance with the specified result.
    /// </summary>
    /// <param name="modal">The reference to the modal instance.</param>
    /// <param name="result">The result to return when dismissing the modal instance.</param>
    internal Task DismissInstance(ModalReference? modal, ModalResult result)
    {
        if (modal == null)
        {
            return Task.CompletedTask;
        }

        modal.Dismiss(result);
        ModalState.RemoveModal(modal);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the modal reference by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the modal instance.</param>
    /// <returns>The modal reference if found; otherwise, null.</returns>
    private ModalReference? GetModalReference(Guid id)
    {
        return ModalState.GetModal(id);
    }

    public override async ValueTask DisposeAsync()
    {
        await _script.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }

    public void UpdateTitle(string title)
    {
        Options.Title = title;
        StateHasChanged();
    }
}
