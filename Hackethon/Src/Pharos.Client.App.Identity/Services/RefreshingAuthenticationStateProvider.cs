using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts;

namespace Pharos.Client.App.Identity.Services;

/// <summary>
/// This is a client-side AuthenticationStateProvider that provides user display name, roles, and permissions
/// for display purposes. Does not affect actual authorization since policies are enforced server-side.
/// </summary>
internal class RefreshingAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ICurrentUserNotificationProvider _currentUserNotificationProvider;
    private readonly Func<Task>? _userInfoChangedHandler;

    private static readonly Task<AuthenticationState> _defaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly bool _isAuthenticated;

    /// <summary>
    /// The claims persisted by the server in the PersistentComponentState see PersistingServerAuthenticationStateProvider class.
    /// </summary>
    private Claim[]? _claims;

    private bool _userInfoChanged;

    public RefreshingAuthenticationStateProvider(PersistentComponentState state, ICurrentUserNotificationProvider currentUserNotificationProvider, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _currentUserNotificationProvider = currentUserNotificationProvider;

        if (!state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
        {
            // if user info is not set by the server, this means the user is not authenticated
            return;
        }

        _isAuthenticated = true;

        _userInfoChangedHandler = () =>
        {
            _userInfoChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateInternal());
            return Task.CompletedTask;
        };

        _currentUserNotificationProvider.UserInfoChanged += _userInfoChangedHandler;

        _claims = GetClaims(userInfo);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_isAuthenticated)
        {
            return _defaultUnauthenticatedTask;
        }

        return GetAuthenticationStateInternal();
    }

    private async Task<AuthenticationState> GetAuthenticationStateInternal()
    {
        try
        {
            if (_userInfoChanged)
            {
                var client = new UsersClient(_httpClient);
                var userInfo = await client.GetMyUserInfoAsync();
                _claims = GetClaims(userInfo);
                _userInfoChanged = false;
            }
        }
        catch
        {
            // Getting user's own info fails if the user is no longer authenticated (e.g. the account has been disabled).
            // We suppress the exception here, because HTTP 401 is handled in AuthorizationHttpMessageHandler.
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(_claims, authenticationType: nameof(RefreshingAuthenticationStateProvider))));
    }

    private static Claim[] GetClaims(UserInfo? userInfo)
    {
        ArgumentNullException.ThrowIfNull(userInfo);

        // map permission to claims checked in policies (program.cs)
        var permissions = userInfo.Permissions?.Select(c => new Claim(Permissions.ClaimType, c)).ToArray() ?? [];

        // Note: pros of having a named type is that we could more easily create custom policies where we combine permissions.

        // built-in roles in Pharos to combine with permissions
        var userRoles = userInfo.Roles?.Select(c => new Claim(ClaimTypes.Role, c)).ToArray() ?? [];

        var claims = new Claim[] {
            new(ClaimTypes.NameIdentifier, userInfo.UserId.ToString()),
            new(ClaimTypes.Name, userInfo.UserName)
        };

        var policies = userInfo.RndMode ? new[] { new Claim(Permissions.ClaimType, Policies.RnD) } : Array.Empty<Claim>();

        claims = [
            .. claims,
            .. permissions, // permission claims
            .. userRoles, // role claims
            .. policies // policy claims
        ];
        return claims;
    }

    public void Dispose()
    {
        if (_userInfoChangedHandler is not null)
        {
            _currentUserNotificationProvider.UserInfoChanged -= _userInfoChangedHandler;
        }
    }
}
