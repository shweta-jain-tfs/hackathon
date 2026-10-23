using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Pharos.Server.Identity.Domain;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

/// <summary>
/// For internal (testing) purposes only. Do not use.
/// </summary>
public interface IApplicationRoleManager
{
    IQueryable<ApplicationRole> Roles { get; }
    Task<IdentityResult> AddClaimAsync(ApplicationRole role, Claim claim);
    Task<IdentityResult> CreateAsync(ApplicationRole role);
    Task<ApplicationRole?> FindByNameAsync(string roleName);
    Task<IList<Claim>> GetClaimsAsync(ApplicationRole role);
    Task<IdentityResult> RemoveClaimAsync(ApplicationRole role, Claim claim);
    Task<IdentityResult> UpdateAsync(ApplicationRole role);
    Task<IdentityResult> DeleteAsync(ApplicationRole role);
    Task<ApplicationRole?> FindByIdAsync(string roleId);
}
