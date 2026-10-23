using Pharos.Client.App.Identity.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.User;

public partial class UserAccountPage
{
    [Inject] private UserAccountPageViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IExceptionHandlerService ExceptionHandler { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ViewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            ExceptionHandler.ShowExceptionMessage(exception);
        }
    }

    private async Task ChangePassword()
    {
        if (ViewModel.CurrentUser == null)
        {
            return;
        }

        var options = new ModalOptions
        {
            Title = Loc["ApplicationUsers_ChangePasswordDialog_Title"],
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Small
        };

        var parameters = new ModalParameters
        {
            { nameof(UserAccountChangePasswordModalView.UserName), ViewModel.CurrentUser.UserName },
            {
                nameof(UserAccountChangePasswordModalView.ChangePasswordAsync),
                async (string currentPassword, string newPassword) => await ChangePasswordCallback(currentPassword, newPassword)
            }
        };

        var modal = Modal.Show<UserAccountChangePasswordModalView>(options, parameters);
        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        try
        {
            await ViewModel.InitializeAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception exception)
        {
            ExceptionHandler.ShowExceptionMessage(exception);
        }
    }

    private async Task ChangePasswordCallback(string currentPassword, string newPassword)
    {
        await ViewModel.ChangeCurrentUserPasswordAsync(currentPassword, newPassword);
    }
}
