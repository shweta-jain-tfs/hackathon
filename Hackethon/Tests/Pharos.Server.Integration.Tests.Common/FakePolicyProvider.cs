using Microsoft.AspNetCore.Authorization;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Integration.Tests.Common;
internal class FakePolicyProvider : PharosPolicyProvider
{
    public override Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return Task.FromResult(new AuthorizationPolicyBuilder(FakeAuthenticationHandler.TestScheme).RequireAuthenticatedUser().Build());
    }
}
