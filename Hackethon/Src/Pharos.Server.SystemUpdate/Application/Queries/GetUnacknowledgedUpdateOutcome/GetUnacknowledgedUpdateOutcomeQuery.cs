using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Queries.GetUnacknowledgedUpdateOutcome;

internal sealed record GetUnacknowledgedUpdateOutcomeQuery : IRequest<SystemUpdateOutcomeDto>;

internal sealed class GetUnacknowledgedUpdateOutcomeQueryHandler
    : IRequestHandler<GetUnacknowledgedUpdateOutcomeQuery, SystemUpdateOutcomeDto>
{
    private readonly ISystemUpdateDbContext _dbContext;
    private readonly IUpdateOutcomeEvaluator _updateEvaluator;
    private readonly ILogger<GetUnacknowledgedUpdateOutcomeQueryHandler> _logger;

    public GetUnacknowledgedUpdateOutcomeQueryHandler(ISystemUpdateDbContext dbContext, IUpdateOutcomeEvaluator updateEvaluator, ILogger<GetUnacknowledgedUpdateOutcomeQueryHandler> logger)
    {
        _dbContext = dbContext;
        _updateEvaluator = updateEvaluator;
        _logger = logger;
    }

    public async Task<SystemUpdateOutcomeDto> Handle(GetUnacknowledgedUpdateOutcomeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Evaluating pending system update outcomes...");

        // Ensure any pending updates are evaluated before fetching the latest unacknowledged update
        await _updateEvaluator.EvaluateUpdateOutcome(cancellationToken);

        var update = await _dbContext.GetLatestUnacknowledgedAsync(cancellationToken);

        SystemUpdateOutcomeDto outcome;

        // Either there are no previous system updates or all updates are already acknowledged
        if (update is null)
        {
            outcome = new SystemUpdateOutcomeDto
            {
                IsAcknowledged = true
            };
        }
        else
        {
            outcome = new SystemUpdateOutcomeDto
            {
                IsAcknowledged = false,
                IsSuccessful = update.Status == UpdateStatus.Successful
            };
        }

        return outcome;
    }
}