using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

internal sealed class ActiveUserAuthorizationHandler(IApplicationUserManager userManager) : AuthorizationHandler<ActiveUserRequirement>
{
    public const string InactiveUserFailureReason = "InactiveUser";

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveUserRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // The cookie or access token is valid, but we need to check if the user is still active in the system.
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userId, out var guid) && !await userManager.IsActive(guid))
            {
                context.Fail(new AuthorizationFailureReason(this, InactiveUserFailureReason));
                return;
            }
        }

        context.Succeed(requirement);
    }
}
