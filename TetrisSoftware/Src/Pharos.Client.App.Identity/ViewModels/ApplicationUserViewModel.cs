namespace Pharos.Client.App.Identity.ViewModels;

public class ApplicationUserViewModel : ViewModelItem<ApplicationUser>, IEquatable<ApplicationUserViewModel>
{
    public ApplicationUserViewModel(ApplicationUser model) : base(model)
    {
    }

    public bool Equals(ApplicationUserViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return Model.Id == other.Model.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ApplicationUserViewModel);
    }

    public override int GetHashCode()
    {
        return Model.Id.GetHashCode();
    }
}