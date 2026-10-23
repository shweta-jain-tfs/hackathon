using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Instruments;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.InstrumentManagement.Views.Instruments;
public partial class InstrumentRenameView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public string DisplayName { get; set; } = default!;

    private InstrumentRenameViewModel RenameModel { get; } = new();
    private PhValidationContext? _validation;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private async Task ValidateAndSave()
    {
        if (_validation?.Validate() != true)
        {
            return;
        }

        await CloseModal(ModalResult.Ok(RenameModel.UpdatedDisplayName));
    }

    private bool IsValid()
    {
        return _validation?.IsValid() ?? false;
    }
}