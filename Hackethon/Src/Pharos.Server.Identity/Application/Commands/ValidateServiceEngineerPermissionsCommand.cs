using MediatR;
using Pharos.Contracts.Security;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Application.Commands;

/// <summary>
/// Use this command to revoke the "Manage Users and Roles" permission from the Service Engineer role
/// if there is any non-service user in the system with this permission.
/// Returns true if the permission was revoked, false otherwise.
/// </summary>
internal record ValidateServiceEngineerPermissionsCommand : IRequest<bool>;

internal class ValidateServiceEngineerPermissionsCommandHandler(
    IApplicationUserManager userManager,
    IApplicationRoleManager roleManager,
    IDateTime dateTime) : IRequestHandler<ValidateServiceEngineerPermissionsCommand, bool>
{
    public async Task<bool> Handle(ValidateServiceEngineerPermissionsCommand request, CancellationToken cancellationToken)
    {
        var serviceEngineerRole = await roleManager.FindByNameAsync(BuiltInRoles.ServiceEngineer).ConfigureAwait(false);
        if (serviceEngineerRole == null)
        {
            return false;
        }

        var serviceEngineerClaims = await roleManager.GetClaimsAsync(serviceEngineerRole).ConfigureAwait(false);
        var manageUsersAndRolesClaim = serviceEngineerClaims.FirstOrDefault(
            x => x is { Type: Permissions.ClaimType, Value: Permissions.ManageUsersAndRoles });

        if (manageUsersAndRolesClaim == null)
        {
            return false;
        }

        var otherRoles = roleManager.Roles
            .Where(role => role.Id != serviceEngineerRole.Id)
            .ToList();

        foreach (var role in otherRoles)
        {
            var roleClaims = await roleManager.GetClaimsAsync(role).ConfigureAwait(false);
            var canManageUsersAndRoles = roleClaims.Any(
                x => x is { Type: Permissions.ClaimType, Value: Permissions.ManageUsersAndRoles });

            if (!canManageUsersAndRoles)
            {
                continue;
            }

            var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!).ConfigureAwait(false);
            foreach (var user in usersInRole)
            {
                var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > dateTime.UtcNow;
                if (!user.IsActive || user.TemporaryPassword || isLockedOut)
                {
                    continue;
                }
                await roleManager.RemoveClaimAsync(serviceEngineerRole, manageUsersAndRolesClaim).ConfigureAwait(false);
                return true;
            }
        }

        return false;
    }
}