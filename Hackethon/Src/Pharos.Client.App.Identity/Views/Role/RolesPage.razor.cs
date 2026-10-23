using Pharos.Client.App.Identity.ViewModels;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.Role;

public partial class RolesPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private RoleListViewModel ViewModel { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    private readonly List<RoleViewModel> _protectedRoles = new();

    private PhDataGrid<RoleViewModel> _phDataGrid = default!;
    private HashSet<RoleViewModel>? _selectedItems;
    private bool _isDeleteAvailable;

    private List<BreadcrumbItem> Breadcrumbs => new(){
        new(Loc["NavMenu_Usersandroles"], PageRoute.UsersAndRoles.Default),
        new(Loc["NavMenu_Roles"], PageRoute.UsersAndRoles.Roles),
    };

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();
        _protectedRoles.Clear();
        _protectedRoles.AddRange(ViewModel.Where(x => x.Model.IsBuiltInRole || x.Model.IncludesCurrentUser));
        _isDeleteAvailable = _selectedItems != null && _selectedItems.Count > 0;
    }

    private void SelectedRoleChanged(HashSet<RoleViewModel> selectedRoles)
    {
        _selectedItems = selectedRoles;
        _isDeleteAvailable = selectedRoles.Count > 0;
    }

    private async Task AddRoleButtonClicked()
    {
        var rolesDisplayNamesWithoutBlankSpaces = ViewModel.Select(x => x.Model.DisplayName.Replace(" ", "").ToLower()).ToList();
        var parameters = new ModalParameters
            {
                { nameof(AddRoleModalView.RoleDisplayNames), rolesDisplayNamesWithoutBlankSpaces }
            };
        var modal = Modal.Show<AddRoleModalView>(
            new ModalOptions
            {
                Title = Loc["RolePage_AddDialog_Title"],
                ModalHeight = ModalSize.Small,
                ModalWidth = ModalSize.XSmall
            }, parameters);

        var result = await modal.Result;

        if (result is { Canceled: false, Data: Common.Models.Role newRole })
        {
            if (!string.IsNullOrEmpty(newRole.DisplayName))
            {
                var id = await AddRole(newRole.DisplayName);
                NavigationManager.NavigateTo(PageRoute.UsersAndRoles.RoleDetails.DetailsPage(id));
            }
        }
    }

    private async Task<Guid> AddRole(string displayName)
    {
        return await ViewModel.CreateAsync(displayName);
    }

    private async Task DeleteRoles()
    {
        if (_selectedItems is { Count: > 0 })
        {
            var parameters = new ModalParameters
            {
                { nameof(DeleteRoleModalView.RoleNames), string.Join(", ", _selectedItems.Select(x => x.Model.DisplayName)) }
            };

            var modal = Modal.Show<DeleteRoleModalView>(new ModalOptions { Title = Loc["RolePage_DeleteDialog_Title"] }, parameters);

            if (!(await modal.Result).Canceled)
            {
                var list = _selectedItems.Select(x => x.Model.Id).ToList();
                await ViewModel.DeleteRolesAsync(list);
                await _phDataGrid.ClearSelectedItem();
                _selectedItems.Clear();
                SelectedRoleChanged(_selectedItems);
                await ViewModel.LoadAsync();
            }
        }
    }

    private static bool MatchesProtectedRole(RoleViewModel role, object obj)
    {
        var currentRole = (RoleViewModel)obj;
        return role.Model.Id == currentRole.Model.Id;
    }
}