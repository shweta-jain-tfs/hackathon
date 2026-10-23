using Pharos.Client.App.Identity.Models;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Identity.ViewModels;

public class ApplicationUserDetailsViewModel : ViewModelItem<ApplicationUser>
{
    private readonly List<string> _allUserNames;
    private readonly IApplicationUserDataService _changePasswordDataService;
    private readonly IStringLocalizer<Translations> _loc;

    public ApplicationUserDetailsViewModel(ApplicationUser model, bool isCurrentUser, bool isPinLoginEnabled, List<Role> roles, List<string> allUserNames, IApplicationUserDataService changePasswordDataService, IStringLocalizer<Translations> loc) : base(model)
    {
        IsCurrentUser = isCurrentUser;
        IsServiceEngineer = model.Role?.Name == BuiltInRoles.ServiceEngineer;
        IsPinLoginEnabled = isPinLoginEnabled;
        Roles = roles;
        _allUserNames = allUserNames;
        _changePasswordDataService = changePasswordDataService;
        _loc = loc;

        RoleDropDownItems =
        [
            new DropDownOption<Guid?>
            {
                Text = "None",
                Value = null
            }
        ];
        RoleDropDownItems.AddRange(
            roles
                .Where(x => x.Name != BuiltInRoles.ServiceEngineer) // you cannot assign Service Engineer role directly
                .OrderBy(x => x.DisplayName)
                .Select(x => new DropDownOption<Guid?>
                {
                    Text = x.DisplayName,
                    Value = x.Id
                }).ToList()
        );
    }

    public List<DropDownOption<Guid?>> RoleDropDownItems { get; }
    public bool IsCurrentUser { get; }
    public List<Role> Roles { get; }

    public string UserName
    {
        get => Model.UserName;
        set => SetProperty(Model.UserName, value, v => Model.UserName = v);
    }

    public string FullName
    {
        get => Model.FullName;
        set => SetProperty(Model.FullName, !string.IsNullOrEmpty(value) ? value : null, v => Model.FullName = v);
    }

    public bool IsActive
    {
        get => Model.IsActive;
        set => SetProperty(Model.IsActive, value, v => Model.IsActive = v);
    }

    public bool IsPinEnabled
    {
        get => Model.IsPinEnabled;
        set => SetProperty(Model.IsPinEnabled, value, v => Model.IsPinEnabled = v);
    }

    public Guid? SelectedRoleId
    {
        get => Model.Role?.Id;
        set
        {
            Model.Role = Roles.FirstOrDefault(x => x.Id == value);

            // Reset the IsPinEnabled flag if the newly selected role does not allow pin enabling
            if (!CanEnablePin && Model.IsPinEnabled)
            {
                Model.IsPinEnabled = false;
            }

            SetAsModified();
        }
    }

    public bool IsUserNameUnique => !_allUserNames.Contains(UserName, StringComparer.OrdinalIgnoreCase);

    public bool CanEnablePin => Model.Role?.Name != BuiltInRoles.ServiceEngineer;

    public IEnumerable<(string PermissionKey, string translatedName)> Permissions
    {
        get
        {
            var permissions = Roles.FirstOrDefault(x => x.Id == SelectedRoleId)?.Permissions ?? [];

            return permissions
                .Select(permission => (
                    PermissionKey: permission,
                    translatedName: _loc[$"Role_{permission}"].ToString()
                ))
                .OrderBy(item => item.translatedName);
        }
    }

    public bool IsPinLoginEnabled { get; }

    public bool IsServiceEngineer { get; }

    public async Task ChangePasswordAsync(string password)
    {
        await _changePasswordDataService.ChangePasswordAsync(Model.Id, password);
    }
}