using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Pharos.Server.Application;

namespace Pharos.Server.Identity.Application.Services;

internal sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IOptions<IdentityOptions> optionsAccessor) : ICurrentUserService
{
    private readonly IdentityOptions _options = optionsAccessor.Value;

    public Guid? UserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(_options.ClaimsIdentity.UserIdClaimType);
            return !Guid.TryParse(userId, out var result) ? null : result;
        }
    }
}
