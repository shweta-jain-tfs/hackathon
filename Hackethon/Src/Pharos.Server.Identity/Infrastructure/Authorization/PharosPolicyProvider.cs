using Microsoft.AspNetCore.Authorization;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

internal class PharosPolicyProvider : IAuthorizationPolicyProvider
{
    /// <summary>
    /// The scheme used to identify combination of <see cref="Microsoft.AspNetCore.Identity.IdentityConstants.BearerScheme"/> and <see cref="Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme"/>.
    /// </summary>
    public const string BearerAndApplicationScheme = "Identity.BearerAndApplication";

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // The authentication scheme for AuthorizationPolicyBuilder is the same as in
        // the IdentityServiceCollectionExtensions.AddIdentityApiEndpoints() method.
        // If you change the main authentication scheme, you need to update the following line.
        var policy = new AuthorizationPolicyBuilder(BearerAndApplicationScheme);
        policy.AddRequirements(new ActiveUserRequirement());
        policy.AddRequirements(new PharosPolicyRequirement(policyName));
        return Task.FromResult(policy.Build())!;
    }

    public virtual Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        var policy = new AuthorizationPolicyBuilder(BearerAndApplicationScheme)
            .RequireAuthenticatedUser();
        policy.AddRequirements(new ActiveUserRequirement());
        return Task.FromResult(policy.Build());
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return Task.FromResult<AuthorizationPolicy?>(null);
    }
}