using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.SystemErrors.Application.Events;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Application.Commands;

internal class CreatePharosErrorCommandHandler : IRequestHandler<CreatePharosErrorCommand>
{
    private readonly ISystemErrorsDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly ISender _sender;

    public CreatePharosErrorCommandHandler(ISystemErrorsDbContext dbContext, IDateTime dateTime, ISender sender)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _sender = sender;
    }

    public async Task Handle(CreatePharosErrorCommand request, CancellationToken cancellationToken)
    {
        var instrumentNumber = string.IsNullOrWhiteSpace(request.InstrumentNumber)
            ? null
            : request.InstrumentNumber;

        var systemError = request.AllowMultiple
            ? null
            : await _dbContext.SystemErrors
                .Where(error => error.Source == ErrorSource.Pharos &&
                                error.Code == request.ErrorCode &&
                                error.SerialNumber == instrumentNumber &&
                                error.Response == null)
                .OrderByDescending(error => error.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

        var isNew = systemError is null;

        if (systemError is null)
        {
            systemError = new SystemError
            {
                ErrorId = Guid.NewGuid(),
                Code = request.ErrorCode,
                Source = ErrorSource.Pharos,
                Count = 1,
                SerialNumber = instrumentNumber,
                PossibleResponses = [ResponseAction.Acknowledge]
            };

            systemError.AddDomainEvent(new SystemErrorCreated(systemError));
            await _dbContext.SystemErrors.AddAsync(systemError, cancellationToken);
        }

        systemError.Classification = request.Classification;
        systemError.Timestamp = _dateTime.Now;
        systemError.Parameters = request.Parameters ?? [];

        if (!isNew)
        {
            systemError.AddDomainEvent(new SystemErrorUpdated(systemError));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.Classification == ErrorClassification.Fatal)
        {
            await RemoveFatalTaskCardBeforeRefreshAsync(instrumentNumber, cancellationToken);
        }

        // Refresh task cards so they reflect the highest severity across all unresolved errors,
        // not just the newly created one.
        await SystemErrorTaskCardSync.RefreshErrorTaskCardsAsync(
            _dbContext,
            _sender,
            [instrumentNumber],
            cancellationToken);
    }

    /// <summary>
    /// Removes the current fatal task card before refresh so a newly created fatal error re-adds and retriggers sound control.
    /// </summary>
    /// <param name="instrumentNumber">The instrument serial number, or <see langword="null"/> for global errors.</param>
    /// <param name="cancellationToken">Cancellation token for command dispatch.</param>
    private async Task RemoveFatalTaskCardBeforeRefreshAsync(string? instrumentNumber, CancellationToken cancellationToken)
    {
        var cardType = instrumentNumber is null
            ? TaskCardType.SystemError
            : TaskCardType.Error;

        await _sender.Send(new RemoveTaskCardCommand(cardType, instrumentNumber), cancellationToken);
    }
}
