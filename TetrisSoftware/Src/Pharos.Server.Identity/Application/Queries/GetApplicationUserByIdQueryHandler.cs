using MediatR;
using Pharos.Server.Application.Identity;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Identity.Application.Queries;

internal class GetApplicationUserByIdQueryHandler : IRequestHandler<GetApplicationUserByIdQuery, ApplicationUserDto?>
{
    private readonly IApplicationUserManager _userManager;
    private readonly IApplicationRoleManager _roleManager;
    private readonly IPermissionProvider _permissionProvider;

    public GetApplicationUserByIdQueryHandler(IApplicationUserManager userManager, IApplicationRoleManager roleManager, IPermissionProvider permissionProvider)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _permissionProvider = permissionProvider;
    }

    public async Task<ApplicationUserDto?> Handle(GetApplicationUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByGuidAsync(request.UserId);

        if (user == null)
        {
            // we cannot throw an exception here, because other modules can store the user ID after it is deleted.
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
            IsActive = user.IsActive,
            IsPinEnabled = user.IsPinEnabled,
            Role = roleDto,
        };
    }
}
