using System.ComponentModel;
using Pharos.Client.App.Identity.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.Role;

public partial class RoleDetailsPage : DetailsPageBase<RoleDetailsViewModel, Common.Models.Role, Guid>
{
    private PhValidationContext? _validation;
    private readonly string _manageUsersAndRolesString = "prm_mng_users_roles";
    private readonly string _accessInstrumentServiceUi = "prm_svc_ui"; // Reserved for built in role

    public RoleDetailsPage(
        RoleDetailsPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
    }

    protected override string ParentPage => PageRoute.UsersAndRoles.Roles;

    protected override string DetailsPage(Guid id)
    {
        return PageRoute.UsersAndRoles.RoleDetails.DetailsPage(id);
    }

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return
        [
            new BreadcrumbItem(Loc["NavMenu_Usersandroles"], PageRoute.UsersAndRoles.Default),
            new BreadcrumbItem(Loc["NavMenu_Roles"], PageRoute.UsersAndRoles.Roles)
        ];
    }

    protected override void AddBreadcrumbForViewModel()
    {
        Breadcrumbs.Add(new BreadcrumbItem(DetailViewModel?.Model.DisplayName ?? Loc["Role_DisplayName"], ""));
    }

    protected override void SubscribeToViewModel()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged += DetailViewModelOnPropertyChanged;
        }
    }

    private void DetailViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RoleDetailsViewModel.IsModified))
        {
            StateHasChanged();
        }
    }

    protected override void Disposing()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
        }
    }

    /// <summary>
    /// Gets the permissions sorted by their translated display name.
    /// Returns tuples of (permission, translatedName).
    /// </summary>
    private IEnumerable<(OptionalPermission Permission, string TranslatedName)>? SortedPermissions
    {
        get
        {
            if (DetailViewModel?.Permissions == null)
            {
                return null;
            }

            return DetailViewModel.Permissions
                .Select(permission => (
                    Permission: permission,
                    TranslatedName: Loc[$"Role_{permission.Name}"].ToString()
                ))
                .OrderBy(item => item.TranslatedName);
        }
    }

    private bool IsValid()
    {
        return _validation?.IsValid() == true;
    }

    protected override async Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        if (DetailViewModel is null)
        {
            return false;
        }

        _validation?.Validate();

        if (!IsValid())
        {
            ShowValidationMessage();
            return false;
        }

        DetailViewModel.ApplyPermissions();
        return await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
    }

    private void ShowValidationMessage()
    {
        var opt = new ModalOptions
        {
            Title = Loc["Validation_ValidationError"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Warning,
        };

        var closeButtonText = Loc["Validation_CloseButtonText"];

        string errorMessage;
        if (DetailViewModel?.IsDisplayNameEmpty == true)
        {
            errorMessage = Loc["Role_DisplayNameRequired"];
        }
        else if (DetailViewModel?.IsDisplayNameInUse == true)
        {
            errorMessage = Loc["Role_Exists"];
        }
        else
        {
            errorMessage = Loc["Validation_ValidationError"];
        }

        Modal.ShowInformation(errorMessage, closeButtonText, opt, true);
    }
}
