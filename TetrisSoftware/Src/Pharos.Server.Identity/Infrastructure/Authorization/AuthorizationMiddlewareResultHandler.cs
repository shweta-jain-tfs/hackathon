using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

internal sealed class AuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly Microsoft.AspNetCore.Authorization.Policy.AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var isInactiveUserFailure = authorizeResult.Forbidden &&
            authorizeResult.AuthorizationFailure?.FailureReasons.Any(r =>
                string.Equals(r.Message, ActiveUserAuthorizationHandler.InactiveUserFailureReason, StringComparison.Ordinal)) == true;

        if (isInactiveUserFailure)
        {
            var hasApplicationCookie = context.Request.Cookies.ContainsKey(IdentityModule.ApplicationCookieName);

            if (hasApplicationCookie) // skip for Bearer tokens, as they are stateless and cannot be signed out
            {
                // If the user is authenticated but not active, sign them out to prevent them from accessing the application.
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                var publisher = context.RequestServices.GetRequiredService<IPublisher>();
                var currentInstrumentService = context.RequestServices.GetRequiredService<ICurrentInstrumentService>();
                await publisher.Publish(new UserLoggedOutEvent(currentInstrumentService.InstrumentId));
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
