using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.Models;
using Pharos.Client.App.Sample.ViewModels;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Sample.Views.Trays;

public partial class AddTrayView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Inject] private AddTrayViewModel ViewModel { get; set; } = default!;

    private PhSimpleValidationContext? _validation;

    private const int _maxTrayIdLength = SampleConstants.TrayIdLength;

    private void OnValidationContextStateChanged(ValidationContextState _)
    {
        if (!string.IsNullOrEmpty(ViewModel.ErrorMessage))
        {
            ViewModel.ErrorMessage = string.Empty;
            StateHasChanged();
        }
    }

    private async Task ValidateAndSave()
    {
        if (!_validation?.Validate() ?? false)
        {
            return;
        }

        var success = await ViewModel.CreateTrayAsync();

        if (success)
        {
            await CloseModal(ModalResult.Ok());
        }
        else
        {
            StateHasChanged();
        }
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private bool IsValid()
    {
        var validValue = _validation?.IsValid() ?? true;
        return validValue && string.IsNullOrEmpty(ViewModel.ErrorMessage);
    }
}