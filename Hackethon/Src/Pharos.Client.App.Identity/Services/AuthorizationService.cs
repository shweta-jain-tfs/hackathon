using Microsoft.AspNetCore.Components.Authorization;

namespace Pharos.Client.App.Identity.Services;

internal class AuthorizationService : Common.App.Interfaces.IAuthorizationService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly Microsoft.AspNetCore.Authorization.IAuthorizationService _authService;

    public AuthorizationService(AuthenticationStateProvider authStateProvider, Microsoft.AspNetCore.Authorization.IAuthorizationService authService)
    {
        _authStateProvider = authStateProvider;
        _authService = authService;
    }

    public async Task<bool> IsAuthorized(string permission)
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();

        var authResult = await _authService.AuthorizeAsync(state.User, resource: null, policyName: permission);

        return authResult.Succeeded;
    }

    public async Task<bool> IsServiceRole()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.IsInRole(BuiltInRoles.ServiceEngineer);
    }

    public async Task<bool> IsRndMode()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var rndMode = state.User.HasClaim(c => c is { Type: Permissions.ClaimType, Value: Policies.RnD });
        return rndMode;
    }
}