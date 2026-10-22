using System.ComponentModel;
using Pharos.Client.App.Identity.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Identity.Views.User;

public partial class ApplicationUserDetailsPage : DetailsPageBase<ApplicationUserDetailsViewModel, ApplicationUser, Guid>
{
    private PhValidationContext? _validation;

    public ApplicationUserDetailsPage(
        ApplicationUserDetailsPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
    }

    private async Task ChangePassword()
    {
        if (DetailViewModel is null)
        {
            return;
        }

        var options = new ModalOptions
        {
            Title = Loc["ApplicationUsers_ChangePasswordDialog_Title"],
            ModalHeight = ModalSize.Medium,
            ModalWidth = ModalSize.Small
        };
        var parameters = new ModalParameters
            {
                { nameof(ChangePasswordModalView.UserName), DetailViewModel.UserName }
            };
        var modal = Modal.Show<ChangePasswordModalView>(options, parameters);
        var result = await modal.Result;

        if (!result.Canceled && result.Data is string password && !string.IsNullOrEmpty(password))
        {
            await DetailViewModel.ChangePasswordAsync(password);
            // Show toaster when user is added
            var toasterText = string.Format(@Loc["ApplicationUsersPage_AddDialog_PasswordChanged"], DetailViewModel.UserName);
            Toaster.ShowToast(ToastType.Confirmation, toasterText);
        }
    }

    private bool IsModelValid()
    {
        return _validation?.IsValid() ?? false;
    }

    protected override string ParentPage => PageRoute.UsersAndRoles.Users;

    protected override string DetailsPage(Guid id)
    {
        return PageRoute.UsersAndRoles.UserDetails.DetailsPage(id);
    }

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return
        [
          new BreadcrumbItem(Loc["NavMenu_Usersandroles"], PageRoute.UsersAndRoles.Default),
          new BreadcrumbItem(Loc["NavMenu_Users"], PageRoute.UsersAndRoles.Users)
        ];
    }

    protected override void AddBreadcrumbForViewModel()
    {
        Breadcrumbs.Add(new BreadcrumbItem(DetailViewModel!.Model.UserName ?? Loc["ApplicationUsers_UserName"], ""));
    }

    protected override void SubscribeToViewModel()
    {
        DetailViewModel!.PropertyChanged += DetailViewModelOnPropertyChanged;
    }

    private void DetailViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ApplicationUserDetailsViewModel.IsModified))
        {
            StateHasChanged();
        }
    }

    protected override async Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        if (DetailViewModel is null)
        {
            return false;
        }

        _validation?.Validate();

        if (!(_validation?.IsValid() ?? false))
        {
            return false;
        }

        return await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
    }

    protected override void Disposing()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
        }
    }
}