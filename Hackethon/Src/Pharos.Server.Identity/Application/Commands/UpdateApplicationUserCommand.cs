using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Identity.Infrastructure.Hubs;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Application.Commands;

#pragma warning disable CA1863 // Use 'CompositeFormat'

public record UpdateApplicationUserCommand(string UserName, string? FullName, Guid UserId, bool IsActive, Guid? RoleId, bool IsPinEnabled) : IRequest
{
    [ProtectedData]
    public string? FullName { get; init; } = FullName;
}

internal sealed class UpdateApplicationUserCommandHandler(
    IApplicationUserManager userManager,
    IApplicationRoleManager roleManager,
    ICurrentUserService currentUserService,
    IAuthorizationService authorizationService,
    IOptionsSnapshot<SecuritySettings> securitySettings,
    ICurrentUserServerHub currentUserHub) : IRequestHandler<UpdateApplicationUserCommand>
{
    public async Task Handle(UpdateApplicationUserCommand request, CancellationToken cancellationToken)
    {
        CreateApplicationUserCommandHandler.ValidateUsername(request.UserName);

        var existingUser = await userManager.FindByGuidAsync(request.UserId);

        if (existingUser == null)
        {
            throw new ObjectGoneException(string.Format(Translations.UpdateApplicationUser_UserDeleted, request.UserName));
        }

        var duplicateUser = await userManager.FindByNameAsync(request.UserName);
        if (duplicateUser != null && duplicateUser.Id != request.UserId)
        {
            throw new ValidationException(Translations.UsernameExists);
        }

        // role change validation
        var newRoleName = string.Empty;
        if (request.RoleId.HasValue)
        {
            var newRole = roleManager.Roles.FirstOrDefault(x => x.Id == request.RoleId);
            if (newRole == null)
            {
                throw new ValidationException(Translations.UpdateApplicationUser_RoleDoesNotExist);
            }

            newRoleName = newRole.Name ?? string.Empty;
        }

        var existingRoles = await userManager.GetRolesAsync(existingUser);
        var roleChanged = !existingRoles.Contains(newRoleName);

        if (roleChanged)
        {
            if (existingUser.Id == currentUserService.UserId)
            {
                throw new ValidationException(Translations.UpdateApplicationUser_CannotUpdateOwnRole);
            }

            if (newRoleName == BuiltInRoles.RndUser && !authorizationService.RndMode)
            {
                throw new ValidationException(Translations.UpdateApplicationUser_RndRoleRequiresRndMode);
            }

            if (newRoleName == BuiltInRoles.ServiceEngineer)
            {
                throw new ValidationException(string.Format(Translations.UpdateApplicationUser_ServiceEngineerRoleCannotBeAssignedDirectly, Translations.ApplicationRole_ServiceEngineer));
            }

            if (existingRoles.Contains(BuiltInRoles.ServiceEngineer))
            {
                throw new ValidationException(string.Format(Translations.UpdateApplicationUser_ServiceEngineerRoleCannotBeRevoked, Translations.ApplicationRole_ServiceEngineer));
            }
        }

        if (!request.IsActive && existingUser.IsActive
            && (existingUser.Id == currentUserService.UserId || existingRoles.Contains(BuiltInRoles.ServiceEngineer)))
        {
            throw new ValidationException(Translations.UpdateApplicationUser_CannotDeactivateUser);
        }

        if (securitySettings.Value.EnablePinLogin)
        {
            // Service Engineers cannot have PIN enabled
            if (request.IsPinEnabled && existingRoles.Contains(BuiltInRoles.ServiceEngineer))
            {
                throw new ValidationException(Translations.SetPin_ServiceEngineerPinNotAllowed);
            }
            existingUser.IsPinEnabled = request.IsPinEnabled;

            // this is a way to reset a PIN for a user: disable PIN login and then enable it back.
            if (!request.IsPinEnabled)
            {
                existingUser.PinHash = null;
            }
        }

        if (roleChanged)
        {
            if (existingRoles.Count > 0)
            {
                var removeRoleResult = await userManager.RemoveFromRolesAsync(existingUser, existingRoles);
                if (!removeRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(Translations.UpdateApplicationUser_FailedToRemoveRole);
                }
            }

            if (!string.IsNullOrEmpty(newRoleName))
            {
                var addRoleResult = await userManager.AddToRoleAsync(existingUser, newRoleName);
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(Translations.UpdateApplicationUser_FailedToAssignRole);
                }
            }
        }

        existingUser.UserName = request.UserName;
        existingUser.FullName = request.FullName;
        existingUser.IsActive = request.IsActive;

        var result = await userManager.UpdateAsync(existingUser);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(Translations.UpdateApplicationUser_FailedToUpdateUserProperties);
        }

        await currentUserHub.UserInfoChanged(existingUser.Id);
    }
}

