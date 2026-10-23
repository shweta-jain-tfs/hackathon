using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Common.Views;
public partial class ImportPackageView
{
    [Inject]
    public IStringLocalizer<Translations> Loc { get; set; } = default!;

    /// <summary>
    /// Gets or sets the modal instance.
    /// </summary>
    [CascadingParameter] public ModalInstance ModalInstance { get; set; } = default!;

    /// <summary>
    /// Gets or sets the description text for the import package.
    /// </summary>
    [Parameter] public string? ImportPackageDescriptionText { get; set; }

    private async Task CloseModal(ModalResult result)
    {
        await ModalInstance.CloseAsync(result);
    }
}