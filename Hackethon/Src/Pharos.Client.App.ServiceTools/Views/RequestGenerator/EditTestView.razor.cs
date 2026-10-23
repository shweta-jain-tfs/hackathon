using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.Services;
using Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.ServiceTools.Views.RequestGenerator;

public partial class EditTestView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private EditTestViewModel ViewModel { get; set; } = null!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public RequestGeneratorTest Test { get; set; } = default!;

    private PhSimpleValidationContext? _validation;

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.InitializeAsync(Test);
    }

    private async Task OnClose(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private bool IsValid()
    {
        return (_validation?.IsValid() ?? false) && ViewModel.HasChanges;
    }

    private async Task ValidateAndSave()
    {
        if (!(_validation?.Validate() ?? false))
        {
            return;
        }

        await OnClose(ModalResult.Ok(ViewModel.EditableTest));
    }
}
