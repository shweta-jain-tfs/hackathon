using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.SystemUpdate.Application.Interfaces;

namespace Pharos.Server.SystemUpdate.Application.Events;

internal class ApplicationStartedEventHandler(IUpdateOutcomeEvaluator updateChecker, ILogger<ApplicationStartedEventHandler> logger) : INotificationHandler<ApplicationStartedEvent>
{
    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Evaluating pending system update outcomes...");

        await updateChecker.EvaluateUpdateOutcome(cancellationToken);
    }
}
