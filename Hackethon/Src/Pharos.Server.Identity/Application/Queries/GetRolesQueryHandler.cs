using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Security;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Identity.Application.Queries;

internal class GetRolesQueryHandler(
    IApplicationRoleManager roleManager,
    IApplicationUserManager userManager,
    IPermissionProvider permissionProvider,
    ICurrentUserService currentUserService,
    IAuthorizationService authorizationService) : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleManager.Roles.ToListAsync(cancellationToken);

        var usersInRole = new Dictionary<string, int>();
        var rolePermissions = new Dictionary<string, List<string>>();
        var roleIncludesCurrentUser = new Dictionary<string, bool>();

        foreach (var role in roles.OfType<ApplicationRole>())
        {
            if (role.Name != null)
            {
                var users = await userManager.GetUsersInRoleAsync(role.Name);
                var userCount = users.Count;
                usersInRole[role.Name] = userCount;

                var permissions = (await permissionProvider.GetPermissionClaims(role))?.Select(x => x.Value).ToList();
                rolePermissions[role.Name] = permissions ?? [];

                roleIncludesCurrentUser[role.Name] = users.Any(x => x.Id == currentUserService.UserId);
            }
        }

        return roles.Where(x => x.Name != BuiltInRoles.RndUser || authorizationService.RndMode) // hide R&D role in production mode
            .Select(x =>
            new RoleDto
            {
                Id = x.Id,
                Name = x.Name,
                DisplayName = x.DisplayName,
                UsersInRole = x.Name != null ? usersInRole[x.Name] : 0,
                IsBuiltInRole = x.IsBuiltInRole,
                Permissions = x.Name != null ? rolePermissions[x.Name] : null,
                IncludesCurrentUser = x.Name != null && roleIncludesCurrentUser[x.Name],
            }).ToList();
    }
}
