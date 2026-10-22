using Pharos.Client.App.Identity.Helpers;
using Pharos.Client.App.Identity.ViewModels;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.User;

public partial class AddUserModalView
{

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public List<string> CurrentUserNames { get; set; } = default!;

    private List<string>? _passwordComplexityList;
    private PhSimpleValidationContext? _validation;

    private NewUserViewModel ViewModel { get; } = new();

    protected override async Task OnInitializedAsync()
    {
        var uiSettings = await UiSettingsService.GetSettingsAsync();
        ViewModel.PasswordPolicyContext = new PasswordPolicyContext(uiSettings);
        ViewModel.ExistingUserNames = new HashSet<string>(CurrentUserNames, StringComparer.OrdinalIgnoreCase);
        await base.OnInitializedAsync();
        _passwordComplexityList = PasswordHelper.PasswordComplexityGuiList(uiSettings, Loc);
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private async Task CloseWithResult()
    {
        if (_validation?.Validate() != true)
        {
            return;
        }

        var result = new AddUserModalResult
        (
            UserName: ViewModel.UserName,
            Password: ViewModel.Password
        );

        await CloseModal(ModalResult.Ok(result));
    }

    private bool IsValid()
    {
        return !string.IsNullOrEmpty(ViewModel.UserName) && (_validation?.IsValid() ?? true);
    }
}