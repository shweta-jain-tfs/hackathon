using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.Common.App.Views;
public partial class ExceptionHandlerServiceModalView
{
    /// <summary>
    /// Gets or sets the modal instance.
    /// </summary>
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    /// <summary>
    /// Gets or sets the error message to be displayed.
    /// </summary>
    [Parameter]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the text for the OK button.
    /// </summary>
    [Parameter]
    public string? OkButton { get; set; }
    
    /// <summary>
    /// Invokes the OnClose callback when the close button is clicked.
    /// </summary>
    /// <param name="result">The result to return when closing the modal dialog.</param>
    private async Task Close(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}