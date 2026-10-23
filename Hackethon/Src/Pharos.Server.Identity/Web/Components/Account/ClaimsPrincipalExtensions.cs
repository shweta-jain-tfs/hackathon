using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Pharos.Contracts;
using Pharos.Contracts.Security;

namespace Pharos.Server.Identity.Web.Components.Account;

internal static class ClaimsPrincipalExtensions
{
    public static UserInfo? GetUserInfo(this ClaimsPrincipal principal, IdentityOptions identityOptions, bool rndMode)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.FindFirst(identityOptions.ClaimsIdentity.UserIdClaimType)?.Value;
            var displayName = principal.FindFirst(ClaimTypes.GivenName)?.Value;

            // AUTH: send permissions to client
            var claims = principal.Claims.ToList();
            var permissions = claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToList();
            var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
            if (rndMode || roles.Contains(BuiltInRoles.ServiceEngineer))
            {
                permissions.Add(Policies.ServiceEngineerOrRndMode);
            }

            if (userId != null && displayName != null && Guid.TryParse(userId, out var userGuid))
            {
                return new UserInfo
                {
                    UserId = userGuid,
                    UserName = displayName,
                    Permissions = permissions.ToArray(),
                    Roles = roles,
                    RndMode = rndMode
                };
            }
        }
        return null;
    }
}
