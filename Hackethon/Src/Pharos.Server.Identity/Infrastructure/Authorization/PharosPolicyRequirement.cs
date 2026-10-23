using Microsoft.AspNetCore.Authorization;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

internal record PharosPolicyRequirement(string Policy) : IAuthorizationRequirement;