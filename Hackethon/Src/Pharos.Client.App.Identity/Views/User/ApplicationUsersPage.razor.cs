using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Pharos.Client.App.Identity.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.User;

public partial class ApplicationUsersPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private ApplicationUsersListViewModel ViewModel { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IDateTime DateTime { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private readonly List<ApplicationUserViewModel> _disableUsers = new();

    private PhDataGrid<ApplicationUserViewModel> _phDataGrid = default!;

    private HashSet<ApplicationUserViewModel>? _selectedItems;
    private bool _isDeleteAvailable;

    private List<BreadcrumbItem> Breadcrumbs => new()
    {
        new(Loc["NavMenu_Usersandroles"], PageRoute.UsersAndRoles.Default),
        new(Loc["NavMenu_Users"], PageRoute.UsersAndRoles.Users)
    };

    private string GetStatus(ApplicationUser user)
    {
        if (user.LockoutEnd != null && DateTime.Now < user.LockoutEnd)
        {
            return Loc["ApplicationUsers_Locked"];
        }
        if (user is { IsActive: true, LastLoginDateTimeOffset: not null })
        {
            return Loc["ApplicationUsers_Active"];
        }

        return Loc["ApplicationUsers_Inactive"];
    }

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();
        var state = await AuthStateProvider.GetAuthenticationStateAsync();
        var userId = Guid.Parse(state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
        _disableUsers.Clear();
        _disableUsers.Add(ViewModel.First(x => x.Model.Id == userId));
        _isDeleteAvailable = _selectedItems is { Count: > 0 };
    }

    private void SelectedUserChanged(HashSet<ApplicationUserViewModel> selectedUser)
    {
        _selectedItems = selectedUser;
        _isDeleteAvailable = selectedUser.Count > 0;
    }

    private async Task AddUserButtonClicked()
    {
        var options = new ModalOptions
        {
            Title = Loc["ApplicationUsersPage_AddDialog_Title"],
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Small
        };

        var userNames = ViewModel.Select(x => x.Model.UserName).ToList();
        var parameters = new ModalParameters
            {
                { nameof(AddUserModalView.CurrentUserNames), userNames }
            };
        var modal = Modal.Show<AddUserModalView>(options, parameters);

        var result = await modal.Result;

        if (!result.Canceled)
        {
            var user = result.Data;
            if (user is AddUserModalResult newUser)
            {
                if (!string.IsNullOrEmpty(newUser.Password) && !string.IsNullOrEmpty(newUser.UserName))
                {
                    await AddUser(newUser.UserName, newUser.Password);
                }
            }
        }
    }

    private async Task AddUser(string userName, string password)
    {
        var userId = await ViewModel.CreateAsync(userName, password);
        if (userId != Guid.Empty)
        {
            NavigationManager.NavigateTo(PageRoute.UsersAndRoles.UserDetails.DetailsPage(userId));
        }
    }

    private async Task DeleteUsers()
    {
        if (_selectedItems != null && _selectedItems.Count > 0)
        {
            var userNames = _selectedItems.Select(x => x.Model.UserName).ToArray();

            var parameters = new ModalParameters
            {
                { nameof(DeleteUserModalView.UserNames), string.Join(", ", userNames) }
            };
            var modal = Modal.Show<DeleteUserModalView>(new ModalOptions { Title = Loc["ApplicationUsersPage_DeleteDialog_Title"] }, parameters);
            var result = await modal.Result;

            if (!result.Canceled)
            {
                var list = _selectedItems.Select(x => x.Model.Id).ToArray();
                await ViewModel.DeleteUsers(list);
                await _phDataGrid.ClearSelectedItem();
                _selectedItems.Clear();
                SelectedUserChanged(_selectedItems);

                await ViewModel.LoadAsync();
            }
        }
    }

    private static bool MatchesDisabledUser(ApplicationUserViewModel user, object obj)
    {
        var currentUser = (ApplicationUserViewModel)obj;
        return user.Model.Id == currentUser.Model.Id;
    }
}
