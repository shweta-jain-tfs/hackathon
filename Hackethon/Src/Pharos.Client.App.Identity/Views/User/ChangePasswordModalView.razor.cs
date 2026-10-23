using Pharos.Client.App.Identity.Helpers;
using Pharos.Client.App.Identity.ViewModels;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.User;

public partial class ChangePasswordModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public string UserName { get; set; } = string.Empty;

    private List<string>? _passwordComplexityList;
    private PhSimpleValidationContext? _validation;

    private NewPasswordViewModel ViewModel { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        ViewModel.UserName = UserName;
        var uiSettings = await UiSettingsService.GetSettingsAsync();
        ViewModel.PasswordPolicyContext = new PasswordPolicyContext(uiSettings);
        await base.OnInitializedAsync();
        _passwordComplexityList = PasswordHelper.PasswordComplexityGuiList(uiSettings, Loc);
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private async Task Save()
    {
        if (_validation?.Validate() != true)
        {
            return;
        }

        await CloseModal(ModalResult.Ok(ViewModel.TemporaryPassword));
    }

    private bool IsValid()
    {
        return _validation?.IsValid() ?? true;
    }
}
