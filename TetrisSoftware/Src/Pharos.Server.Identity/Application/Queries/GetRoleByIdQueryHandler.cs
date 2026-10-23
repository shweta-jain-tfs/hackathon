using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Identity.Application.Queries;
internal class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto?>
{
    private readonly IApplicationRoleManager _roleManager;
    private readonly IPermissionProvider _permissionProvider;
    private readonly Guid? _currentUserId;
    private readonly IApplicationUserManager _userManager;

    public GetRoleByIdQueryHandler(IApplicationRoleManager roleManager, IApplicationUserManager userManager, IPermissionProvider permissionProvider, ICurrentUserService currentUserService/*, IIdentityDbContext dbContext*/)
    {
        _roleManager = roleManager;
        _permissionProvider = permissionProvider;
        _currentUserId = currentUserService.UserId;
        _userManager = userManager;
    }

    public async Task<RoleDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());

        if (role != null)
        {
            var permissions = (await _permissionProvider.GetPermissionClaims(role))?.Select(x => x.Value).ToList();
            var usersInRole = role.Name != null ? await _userManager.GetUsersInRoleAsync(role.Name) : null;
            var roleIncludesCurrentUser = usersInRole?.Any(x => x.Id == _currentUserId) ?? false;
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                UsersInRole = null,
                IsBuiltInRole = role.IsBuiltInRole,
                Permissions = permissions,
                IncludesCurrentUser = roleIncludesCurrentUser,
            };
        }
        throw new NotFoundException();
    }
}
