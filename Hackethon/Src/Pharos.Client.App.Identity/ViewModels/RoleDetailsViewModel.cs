namespace Pharos.Client.App.Identity.ViewModels;

public class OptionalPermission
{
    private readonly Action _setIsModified;
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                _setIsModified();
            }
        }
    }
    public string Name { get; }
    public OptionalPermission(string name, bool isSelected, Action isModifiedAction)
    {
        Name = name;
        _isSelected = isSelected;
        _setIsModified = isModifiedAction;
    }
}

public sealed class RoleDetailsViewModel : ViewModelItem<Role>
{
    private readonly List<string> _roleNames;

    public RoleDetailsViewModel(Role model, List<string> otherRoleNames) : base(model)
    {
        _roleNames = otherRoleNames;

        Permissions = Contracts.Security.Permissions.All
            .Select(permission => new OptionalPermission(permission, model.Permissions.Contains(permission), SetAsModified))
            .ToList();
    }

    public List<OptionalPermission> Permissions { get; }

    public string DisplayName
    {
        get => Model.DisplayName;
        set
        {
            if (value.Length == RolesContract.MaximumNameLength)
            {
                return;
            }
            SetProperty(Model.DisplayName, value, v => Model.DisplayName = v);
        }
    }

    public bool IsDisplayNameInUse => _roleNames.Any(x => x.Equals(DisplayName, StringComparison.OrdinalIgnoreCase));

    public bool IsDisplayNameEmpty => string.IsNullOrWhiteSpace(Model.DisplayName);

    public void ApplyPermissions()
    {
        Model.Permissions = Permissions.Where(p => p.IsSelected).Select(p => p.Name).ToArray();
    }
}