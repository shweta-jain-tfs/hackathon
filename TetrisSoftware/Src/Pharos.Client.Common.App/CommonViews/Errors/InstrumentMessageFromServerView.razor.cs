using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.Common.App.CommonViews.Errors;
public partial class InstrumentMessageFromServerView
{

    /// <summary>
    /// Gets or sets the instance of the modal dialog.
    /// </summary>
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    /// <summary>
    /// Gets or sets the message text to be displayed in the modal dialog.
    /// </summary>
    [Parameter] public string? MessageText { get; set; }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}