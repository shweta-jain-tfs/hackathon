using Microsoft.Extensions.Options;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Queries;

internal sealed class GetReflexDefinitionsForReflexNameQueryHandler(IReflexDbContext context, IOptionsSnapshot<RequestSettings> reflexOptions)
    : IRequestHandler<GetReflexDefinitionsForReflexNameQuery, IReflexDefinition?>
{
    public async Task<IReflexDefinition?> Handle(GetReflexDefinitionsForReflexNameQuery request, CancellationToken cancellationToken)
    {
        var settings = reflexOptions.Value;
        if (!settings.ReflexTestingEnabled)
        {
            return null;
        }

        var requestorId = string.IsNullOrWhiteSpace(request.RequestorId) ? null : request.RequestorId;

        if (!settings.IsRequestorInformationEnabled)
        {
            requestorId = null;
        }

        // A reflex-named LIS trigger candidate is valid only when an active reflex definition
        // exists for the same reflex name, normalized trigger test/method, and compatible requestor.
        var reflexDefinition = await context.ReflexDefinitions
            .Include(r => r.ReflexCondition)
            .FirstOrDefaultAsync(
                r =>
                    r.Active &&
                    r.Name == request.ReflexName &&
                    r.ReflexCondition.TestName == request.TestName &&
                    r.ReflexCondition.MethodName == request.MethodName &&
                    (r.RequestorId == null || r.RequestorId == requestorId),
                cancellationToken)
            .ConfigureAwait(false);

        return reflexDefinition;
    }
}