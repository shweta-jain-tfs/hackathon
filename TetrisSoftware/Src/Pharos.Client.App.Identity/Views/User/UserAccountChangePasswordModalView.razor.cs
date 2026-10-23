using Pharos.Client.App.Identity.Helpers;
using Pharos.Client.App.Identity.ViewModels;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.User;

public partial class UserAccountChangePasswordModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [Inject] private IExceptionHandlerService ExceptionHandler { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public Func<string, string, Task> ChangePasswordAsync { get; set; } = default!;

    private List<string>? _passwordComplexityList;
    private PhSimpleValidationContext? _validation;

    private ChangePasswordViewModel ViewModel { get; } = new();


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

        try
        {
            await ChangePasswordAsync(ViewModel.CurrentPassword, ViewModel.NewPassword);
            await CloseModal(ModalResult.Ok());
        }
        catch (ApiException ex)
        {
            // Don't close the dialog if the user enters the wrong current password
            // so the user can correct the problem without starting over.
            ExceptionHandler.ShowExceptionMessage(ex);
        }
    }

    private bool IsSaveDisabled()
    {
        return string.IsNullOrWhiteSpace(ViewModel.CurrentPassword)
               || _validation?.IsValid() == false;
    }
}
