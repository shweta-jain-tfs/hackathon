using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Identity.Application.Queries;

/// <summary>
/// Handler for retrieving the current logged-in user's account details.
/// </summary>
internal class GetCurrentUserDetailsQueryHandler : IRequestHandler<GetCurrentUserDetailsQuery, ApplicationUserDto?>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationUserManager _userManager;
    private readonly IApplicationRoleManager _roleManager;
    private readonly IPermissionProvider _permissionProvider;
    private readonly IOptionsSnapshot<SecuritySettings> _securitySettings;

    public GetCurrentUserDetailsQueryHandler(
        ICurrentUserService currentUserService,
        IApplicationUserManager userManager,
        IApplicationRoleManager roleManager,
        IPermissionProvider permissionProvider,
        IOptionsSnapshot<SecuritySettings> securitySettings)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
        _roleManager = roleManager;
        _permissionProvider = permissionProvider;
        _securitySettings = securitySettings;
    }

    public async Task<ApplicationUserDto?> Handle(GetCurrentUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        if (!currentUserId.HasValue)
        {
            return null;
        }

        var user = await _userManager.FindByGuidAsync(currentUserId.Value);

        if (user == null)
        {
            return null;
        }

        var rolesNames = await _userManager.GetRolesAsync(user);
        RoleDto? roleDto = null;

        var roleName = rolesNames.FirstOrDefault();
        if (roleName != null)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var permissions = (await _permissionProvider.GetPermissionClaims(role))?.Select(x => x.Value).ToList();

                roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    DisplayName = role.DisplayName,
                    Permissions = permissions,
                    IsBuiltInRole = role.IsBuiltInRole,
                };
            }
        }

        return new ApplicationUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            LockoutEnd = user.LockoutEnd,
            LockoutEnabled = user.LockoutEnabled,
            LastLoginDateTimeOffset = user.LastLoginDateTimeOffset,
            PasswordExpirationDateTimeOffset = ComputeExpiration(user.PasswordEmittedDateTimeOffset, _securitySettings.Value),
            IsActive = user.IsActive,
            IsPinEnabled = user.IsPinEnabled,
            Role = roleDto,
        };
    }

    private static DateTimeOffset? ComputeExpiration(DateTimeOffset? emitted, SecuritySettings settings)
    {
        if (emitted == null || !settings.PasswordExpirationEnabled)
        {
            return null;
        }
        return emitted.Value.AddDays(settings.PasswordExpirationDays);
    }
}
