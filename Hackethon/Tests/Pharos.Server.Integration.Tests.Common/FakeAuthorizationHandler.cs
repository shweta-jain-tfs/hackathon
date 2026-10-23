using Microsoft.AspNetCore.Authorization;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Integration.Tests.Common;

internal class FakeAuthorizationHandler : AuthorizationHandler<PharosPolicyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PharosPolicyRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
