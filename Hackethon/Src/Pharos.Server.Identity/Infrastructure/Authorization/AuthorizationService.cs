using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.RnD;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

internal class AuthorizationService : IAuthorizationService
{
    private readonly IApplicationUserManager _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationRoleManager _roleManager;
    private readonly IPermissionProvider _permissionProvider;
    private readonly IDateTime _dateTime;
    private readonly IOptionsSnapshot<RndSettings> _rndSettings;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(IApplicationUserManager userManager,
        ICurrentUserService currentUserService,
        IApplicationRoleManager roleManager,
        IPermissionProvider permissionProvider,
        IDateTime dateTime,
        IOptionsSnapshot<RndSettings> rndSettings,
        ILogger<AuthorizationService> logger)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _roleManager = roleManager;
        _permissionProvider = permissionProvider;
        _dateTime = dateTime;
        _rndSettings = rndSettings;
        _logger = logger;

    }

    public Task<bool> AuthorizeCurrentUserAsync(params string[] permissions)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Task.FromResult(false);
        }

        return AuthorizeUserAsync(userId.Value, permissions);
    }

    public async Task<bool> AuthorizeUserAsync(Guid userId, params string[] permissions)
    {
        var user = await GetUser(userId);
        if (user == null)
        {
            return false;
        }

        if (permissions.Length == 0)
        {
            return true;
        }

        var userPermissions = await GetUserPermissionsAsync(user);

        foreach (var permission in permissions)
        {
            var result = userPermissions.Contains(permission);
            if (!result)
            {
                _logger.LogDebug("User {UserId} has no {Permission} permission.", userId, permission);
                return false;
            }
        }
        return true;
    }

    public async Task<bool> IsCurrentUserInRoleAsync(string role)
    {
        if (!RndMode && role == BuiltInRoles.RndUser)
        {
            return false;
        }

        var user = await GetUser(_currentUserService.UserId);
        if (user == null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, role);
    }

    private async Task<ApplicationUser?> GetUser(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null || (user is { LockoutEnabled: true, LockoutEnd: not null } && user.LockoutEnd > _dateTime.UtcNow))
        {
            return null;
        }

        return user;
    }

    public bool RndMode => _rndSettings.Value.RndMode;
    public bool RuoMode => _rndSettings.Value.RuoMode;

    private async Task<HashSet<string>> GetUserPermissionsAsync(ApplicationUser user)
    {
        var result = new HashSet<string>();

        var roleNames = await _userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
        {
            return result;
        }

        foreach (var roleName in roleNames)
        {
            if (roleName == BuiltInRoles.RndUser)
            {
                _logger.LogWarning("R&D mode is not implemented properly.");

                if (!RndMode)
                {
                    // R&D role is not active if the mode is disabled
                    continue;
                }
            }

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.LogError("Unknown role {RoleName} of the user {UserId}", roleName, user.Id);
                continue;
            }

            var roleClaims = (await _permissionProvider.GetPermissionClaims(role))?.ToList();
            if (roleClaims is null || roleClaims.Count == 0)
            {
                _logger.LogError("No permission(s) for role {RoleName} of the user {UserId}", roleName, user.Id);
                continue;
            }

            foreach (var claim in roleClaims)
            {
                result.Add(claim.Value);
            }
        }

        return result;
    }
}
