using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.SystemErrors.Application.Events;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Application.Commands;

public record AcknowledgeSystemErrorsCommand(IReadOnlyCollection<int> SystemErrorIds) : IRequest;

internal class AcknowledgeSystemErrorsCommandHandler : IRequestHandler<AcknowledgeSystemErrorsCommand>
{
    private readonly ISystemErrorsDbContext _dbContext;
    private readonly ISender _sender;
    private readonly IDateTime _dateTime;

    public AcknowledgeSystemErrorsCommandHandler(ISystemErrorsDbContext dbContext, ISender sender, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _sender = sender;
        _dateTime = dateTime;
    }

    public async Task Handle(AcknowledgeSystemErrorsCommand request, CancellationToken cancellationToken)
    {
        // Normalize input to avoid processing the same error id more than once.
        var systemErrorIds = request.SystemErrorIds
            .Distinct()
            .ToList();

        // No ids means there is nothing to acknowledge.
        if (systemErrorIds.Count == 0)
        {
            return;
        }

        // Load all requested errors in one query.
        var errors = await _dbContext.SystemErrors
            .Where(x => systemErrorIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        // Fail fast if at least one requested id does not exist.
        if (errors.Count != systemErrorIds.Count)
        {
            var missingId = systemErrorIds.Except(errors.Select(x => x.Id)).First();
            throw new NotFoundException(nameof(SystemError), missingId);
        }

        foreach (var error in errors)
        {
            // Skip errors that already got a response.
            if (error.Response.HasValue)
            {
                continue;
            }

            // Guard domain rules: only acknowledge when this action is allowed.
            if (!error.PossibleResponses.Contains(ResponseAction.Acknowledge))
            {
                throw new ValidationException($"Error with id '{error.Id}' does not contain error response {ResponseAction.Acknowledge}");
            }

            error.Response = ResponseAction.Acknowledge;
            error.ResponseTimestamp = _dateTime.Now;
            error.AddDomainEvent(new SystemErrorUpdated(error));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Refresh related task cards to reflect updated system error states.
        await SystemErrorTaskCardSync.RefreshErrorTaskCardsAsync(
            _dbContext,
            _sender,
            errors.Select(x => x.SerialNumber),
            cancellationToken);
    }
}
