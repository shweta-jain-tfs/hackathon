using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Pharos.Server.Identity.Domain;

namespace Pharos.Server.Identity.Infrastructure.Authorization;
// Wrapper pattern to encapsulate the RoleManager, eg. to use the same approch as UserManager
internal class ApplicationRoleManager : IApplicationRoleManager
{
    private readonly RoleManager<ApplicationRole> _identityRoleManager;
    public ApplicationRoleManager(RoleManager<ApplicationRole> roleManager)
    {
        _identityRoleManager = roleManager;
    }

    public IQueryable<ApplicationRole> Roles => _identityRoleManager.Roles;
    public Task<IdentityResult> AddClaimAsync(ApplicationRole role, Claim claim)
    {
        return _identityRoleManager.AddClaimAsync(role, claim);
    }

    public Task<IdentityResult> CreateAsync(ApplicationRole role)
    {
        if(Roles.Any(x => x.DisplayName == role.DisplayName))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Role already exists." }));
        }
        return _identityRoleManager.CreateAsync(role);
    }

    public Task<ApplicationRole?> FindByNameAsync(string roleName)
    {
        return _identityRoleManager.FindByNameAsync(roleName);
    }

    public Task<IList<Claim>> GetClaimsAsync(ApplicationRole role)
    {
        return _identityRoleManager.GetClaimsAsync(role);
    }

    public Task<IdentityResult> RemoveClaimAsync(ApplicationRole role, Claim claim)
    {
        return _identityRoleManager.RemoveClaimAsync(role, claim);
    }
    public Task<IdentityResult> UpdateAsync(ApplicationRole role)
    {
        return _identityRoleManager.UpdateAsync(role);
    }
    public Task<IdentityResult> DeleteAsync(ApplicationRole role)
    {
        return _identityRoleManager.DeleteAsync(role);
    }
    public Task<ApplicationRole?> FindByIdAsync(string roleId)
    {
        return _identityRoleManager.FindByIdAsync(roleId);
    }
}
