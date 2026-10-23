using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using IAuthorizationService = Pharos.Server.Application.Security.IAuthorizationService;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

internal sealed class PharosPolicyAuthorizationHandler(
    ICurrentUserService currentUserService,
    ICurrentInstrumentService currentInstrumentService,
    IAuthorizationService authorizationService,
    ILogger<PharosPolicyAuthorizationHandler> logger)
    : AuthorizationHandler<PharosPolicyRequirement>
{

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PharosPolicyRequirement requirement)
    {
        switch (requirement.Policy)
        {
            case Policies.InstrumentOnly:
                if (currentInstrumentService.InstrumentId == null || currentUserService.UserId == null)
                {
                    context.Fail();
                    return;
                }
                context.Succeed(requirement);
                return;

            case Policies.InstrumentOnlyAllowAnonymous:
                if (currentInstrumentService.InstrumentId == null)
                {
                    context.Fail();
                    return;
                }
                context.Succeed(requirement);
                return;

            case Policies.RnD:
                logger.LogWarning("R&D mode is not implemented properly.");

                if (authorizationService.RndMode)
                {
                    context.Succeed(requirement);
                    return;
                }
                context.Fail();
                return;

            case Policies.ServiceEngineerOrRndMode:
                if (authorizationService.RndMode || await authorizationService.IsCurrentUserInRoleAsync(BuiltInRoles.ServiceEngineer))
                {
                    context.Succeed(requirement);
                    return;
                }
                context.Fail();
                return;

            case Policies.Ruo:
                logger.LogWarning("RUO mode is not implemented properly.");
                if (authorizationService.RuoMode)
                {
                    context.Succeed(requirement);
                    return;
                }
                context.Fail();
                return;
        }

        var userId = currentUserService.UserId;
        if (userId != null)
        {
            var authorized = await authorizationService.AuthorizeUserAsync(currentUserService.UserId!.Value, requirement.Policy);

            if (authorized)
            {
                context.Succeed(requirement);
                return;
            }
        }

        context.Fail();
    }
}
