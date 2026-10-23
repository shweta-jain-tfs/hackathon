using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class DeleteLaboratoryNamesModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public required string LaboratoryTestNames { get; set; }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
