namespace Pharos.Client.App.Identity.ViewModels;

public class RoleViewModel : ViewModelItem<Role>, IEquatable<RoleViewModel>
{
    public RoleViewModel(Role model) : base(model)
    {
    }

    public bool Equals(RoleViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return Model.Id == other.Model.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RoleViewModel);
    }

    public override int GetHashCode()
    {
        return Model.Id.GetHashCode();
    }
}