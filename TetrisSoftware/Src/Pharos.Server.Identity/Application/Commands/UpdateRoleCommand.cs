using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FluentValidation.Results;
using MediatR;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Application.Commands;

public record UpdateRoleCommand(string DisplayName, string[]? Permissions, Guid RoleId) : IRequest;

internal sealed class UpdateRoleCommandHandler(IApplicationRoleManager roleManager, IApplicationUserManager userManager, IPermissionProvider permissionProvider, ICurrentUserService currentUserService) : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        // Validate DisplayName is not empty
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ValidationException(Translations.RoleNameRequired);
        }

        var role = roleManager.Roles.FirstOrDefault(x => x.Id == request.RoleId);

        if (role != null)
        {
            if (role.IsBuiltInRole)
            {
                throw new ValidationException(Translations.CannotEditBuiltInRole);
            }

            role.DisplayName = request.DisplayName.Trim();
            var result = await roleManager.UpdateAsync(role);

            await UpdatePermissions(request.Permissions, role);

            if (!result.Succeeded)
            {
                var errors = new List<ValidationFailure>();
                foreach (var error in result.Errors)
                {
                    errors.Add(new ValidationFailure(error.Code, error.Description));
                }

                throw new FluentValidation.ValidationException(errors);
            }
        }
        else
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }
    }

    private async Task UpdatePermissions(string[]? permissions, ApplicationRole role)
    {
        if (permissions == null || string.IsNullOrEmpty(role.Name))
        {
            return;
        }
        var users = await userManager.GetUsersInRoleAsync(role.Name);
        var currentUserInRole = users.Any(x => x.Id == currentUserService.UserId);

        var permissionClaims = await permissionProvider.GetPermissionClaims(role);

        if (permissionClaims?.Count > 0)
        {

            // to remove
            var claimsToRemove = permissionClaims.Where(c => !permissions.Contains(c.Value)).ToList();
            if (claimsToRemove.Count > 0)
            {
                if (currentUserInRole && claimsToRemove.Any(claim => claim.Value == Permissions.ManageUsersAndRoles))
                {
                    throw new ValidationException(Translations.CannotRevokeManageUsers);
                }
                foreach (var claim in claimsToRemove)
                {

                    await roleManager.RemoveClaimAsync(role, claim);
                }
            }

            // to add
            var claimsToAdd = permissions?.Where(p => permissionClaims.All(c => c.Value != p)).Select(p => new Claim(Permissions.ClaimType, p)).ToList() ?? new List<Claim>();
            if (claimsToAdd.Count > 0)
            {
                foreach (var claim in claimsToAdd)
                {
                    await roleManager.AddClaimAsync(role, claim);
                }
            }
        }
        else
        {
            var allClaims = permissionProvider.GetAllPermissionClaims();
            foreach (var permission in permissions)
            {
                if (allClaims.Any(claim => claim.Value == permission))
                {
                    await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
                }

            }
        }
    }
}
