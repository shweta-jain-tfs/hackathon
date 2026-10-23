using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.Role;

public partial class AddRoleModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public required List<string> RoleDisplayNames { get; set; } = [];

    private readonly Common.Models.Role _newRole = new();
    private bool IsExistingRole => _newRole?.DisplayName != null ? RoleDisplayNames.Contains(_newRole.DisplayName.Replace(" ", "").ToLower()) : false;
    private bool IsNewRoleInvalid => string.IsNullOrEmpty(_newRole.DisplayName) || IsExistingRole;
    private string ErrorText => IsExistingRole ? Loc["RolePage_AddDialog_RoleExists"] : "";

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
