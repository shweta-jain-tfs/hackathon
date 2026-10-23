using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Security;
using Pharos.Server.Identity.Application.Interfaces;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Identity.Infrastructure.Database;

internal class IdentityDbContextInitializer(
    IApplicationRoleManager roleManager,
    IIdentityDbContext context,
    ILogger<IdentityDbContextInitializer> logger,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer((DbContext)context, dbEncryptionService)
{
    protected override string SchemaName => IdentityDbContext.Schema;

    public override async Task InitializeAsync(RecreateObjectsMode recreateMode)
    {
        await base.InitializeAsync(recreateMode);
        await InitializeRoles();
    }

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }

    public async Task InitializeRoles()
    {
        var anyRoles = await roleManager.Roles.AnyAsync();
        await InitializeBuiltInRole(BuiltInRoles.ServiceEngineer);
        await InitializeBuiltInRole(BuiltInRoles.RndUser);

        if (!anyRoles)
        {
            foreach (var role in PredefinedRoles.All)
            {
                await InitializePredefinedRole(role);
            }
        }
    }
    public async Task InitializePredefinedRole(string roleName)
    {
        var newRole = ApplicationRole.CreatePredefinedOrBuiltInRole(roleName);
        var createdRole = await roleManager.CreateAsync(newRole);
        if (createdRole.Succeeded)
        {
            var defaultPermissions = ApplicationRole.GetPredefinedOrBuiltInRolePermissions(roleName);
            foreach (var claim in defaultPermissions)
            {
                var addedClaims = await roleManager.AddClaimAsync(newRole, claim);
                if (!addedClaims.Succeeded)
                {
                    logger.LogError("The claims could not be added to {Role}. Errors: {Errors}", roleName, string.Join(", ", addedClaims.Errors));
                }
            }
        }
        else
        {
            logger.LogError("The {Role} role could not be created. Errors: {Errors}", roleName, string.Join(", ", createdRole.Errors));
        }
    }
    public async Task InitializeBuiltInRole(string roleName)
    {
        var builtInRole = await roleManager.FindByNameAsync(roleName);
        var defaultPermissions = ApplicationRole.GetPredefinedOrBuiltInRolePermissions(roleName);
        if (builtInRole == null)
        {
            logger.LogInformation("Creating the {Role} role.", roleName);
            builtInRole = ApplicationRole.CreatePredefinedOrBuiltInRole(roleName);
            await roleManager.CreateAsync(builtInRole);

            foreach (var claim in defaultPermissions)
            {
                await roleManager.AddClaimAsync(builtInRole, claim);
            }

            if (roleName == BuiltInRoles.ServiceEngineer)
            {
                // We only want to add the manage users and roles permission to the service engineer role
                // when we are setting up a new system. Later, when the service engineer adds a local admin,
                // we will revoke this permission.
                await roleManager.AddClaimAsync(builtInRole, new Claim(Permissions.ClaimType, Permissions.ManageUsersAndRoles));
            }

            return;
        }

        // if the built-in role exists we want to check that it has the correct permissions
        var existingPermissions = await roleManager.GetClaimsAsync(builtInRole);
        if (roleName == BuiltInRoles.ServiceEngineer)
        {
            var claim = existingPermissions.FirstOrDefault(c => c is { Type: Permissions.ClaimType, Value: Permissions.ManageUsersAndRoles });
            if (claim != null)
            {
                // This is an extra permission that we add above. We don't want it to be removed on Pharos restart.
                existingPermissions.Remove(claim);
            }
        }

        var permissionsAreTheSame = existingPermissions.Count == defaultPermissions.Count;
        if (permissionsAreTheSame)
        {
            foreach (var claim in existingPermissions)
            {
                if (defaultPermissions.FirstOrDefault(c => c.Type == claim.Type && c.Value == claim.Value) == null)
                {
                    permissionsAreTheSame = false;
                    break;
                }
            }
        }

        if (!permissionsAreTheSame)
        {
            logger.LogInformation("The default permissions for the {Role} role have changes. Resetting permissions.", roleName);
            // reset claims to the default state
            foreach (var claim in existingPermissions)
            {
                await roleManager.RemoveClaimAsync(builtInRole, claim);
            }

            foreach (var claim in defaultPermissions)
            {
                await roleManager.AddClaimAsync(builtInRole, claim);
            }
        }
    }
}