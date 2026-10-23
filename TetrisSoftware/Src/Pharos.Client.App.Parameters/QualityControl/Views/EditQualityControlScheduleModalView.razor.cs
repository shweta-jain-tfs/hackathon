using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class EditQualityControlScheduleModalView : IDisposable
{
    private bool _isValid = true;
    private PhSimpleValidationContext? _validationContext;

    [Parameter] [EditorRequired] public EditQualityControlScheduleViewModel ViewModel { get; set; }

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private async Task Cancel()
    {
        await Modal.CloseAsync(ModalResult.Cancel());
    }

    private async Task Save()
    {
        await Modal.CloseAsync(ModalResult.Ok(ViewModel));
    }

    private void ValidationChanged(ValidationContextState obj)
    {
        _isValid = obj.IsValid;
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

    public void Dispose()
    {
        ViewModel.ReValidate -= ValidationNeeded;

        _validationContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}