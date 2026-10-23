using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Contracts.Security;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class MethodClassTableDetailsModalView : IDisposable
{
    [Parameter, EditorRequired] public MethodClassTableDetailsViewModel ViewModel { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private PhSimpleValidationContext? _validationContext;
    private bool IsEditingDisabled { get; set; } = true;
    private bool _isValid;

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private async Task ValidateAndApply()
    {
        if (_validationContext?.Validate() != true)
        {
            return;
        }

        await CloseModal(ModalResult.Ok(ViewModel));
    }

    private void OnTextChanged(MethodClassTableDetailsRowViewModel row, string? value)
    {
        row.Text = value;
        _validationContext?.Validate();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (!firstRender)
        {
            return;
        }

        _validationContext?.Validate();
        _isValid = _validationContext?.IsValid() ?? false;
        StateHasChanged();
    }

    private void ValidationChanged(ValidationContextState state)
    {
        _isValid = state.IsValid;
    }

    public void Dispose()
    {
        _validationContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}
