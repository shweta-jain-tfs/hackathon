using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Pharos.Client.App.Identity.Models;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.Identity.ViewModels;

public class ApplicationUserDetailsPageViewModel : DetailsPageViewModel<ApplicationUserDetailsViewModel, ApplicationUser, Guid>
{
    private readonly IApplicationUserDataService _dataService;
    private readonly IRoleDataService _roleDataService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly UiSettingsService _uiSettingsService;
    private readonly IStringLocalizer<Translations> _loc;

    public ApplicationUserDetailsPageViewModel(IApplicationUserDataService dataService, IRoleDataService roleDataService, AuthenticationStateProvider authStateProvider, UiSettingsService uiSettingsService, IStringLocalizer<Translations> loc) : base(dataService, null)
    {
        _dataService = dataService;
        _roleDataService = roleDataService;
        _authStateProvider = authStateProvider;
        _uiSettingsService = uiSettingsService;
        _loc = loc;
    }

    protected override Task<ApplicationUser> CreateDefaultModel()
    {
        return Task.FromResult(new ApplicationUser());
    }

    protected override async Task<ApplicationUserDetailsViewModel> CreateViewModel(ApplicationUser model)
    {
        var authStateTask = _authStateProvider.GetAuthenticationStateAsync();
        var rolesTask = _roleDataService.GetAsync();
        var usernameListTask = _dataService.GetUserNamesAsync();
        var settingsTask = _uiSettingsService.GetSettingsAsync();

        await Task.WhenAll(authStateTask, rolesTask, usernameListTask, settingsTask);

        var isCurrentUser = false;

        var userIdClaim = authStateTask.Result.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var currentUserId))
        {
            isCurrentUser = model.Id == currentUserId;
        }

        var roles = rolesTask.Result.ToList();
        var allOtherUserNames = usernameListTask.Result.Where(x => x != model.UserName).ToList();

        var isPinLoginEnabled = settingsTask.Result.SecuritySettings.EnablePinLogin;

        return new ApplicationUserDetailsViewModel(model, isCurrentUser, isPinLoginEnabled, roles, allOtherUserNames, _dataService, _loc);
    }
}