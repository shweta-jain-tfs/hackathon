using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class EditQualityControlTestModalView : IDisposable
{
    private bool _isValid;
    private PhSimpleValidationContext? _validationContext;

    [Parameter, EditorRequired] public EditQualityControlTestViewModel ViewModel { get; set; }

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private async Task Apply()
    {
        await Modal.CloseAsync(ModalResult.Ok(ViewModel));
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ViewModel.ReValidate += ValidationNeeded;
    }

    private void ValidationNeeded(object? sender, EventArgs e)
    {
        _validationContext?.Validate();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            _validationContext?.Validate();
            _isValid = _validationContext?.IsValid() ?? false;
            StateHasChanged();
        }
    }

    private void ValidationChanged(ValidationContextState obj)
    {
        _isValid = obj.IsValid;
    }

    public void Dispose()
    {
        _validationContext?.Dispose();
        ViewModel.ReValidate -= ValidationNeeded;
        GC.SuppressFinalize(this);
    }
}