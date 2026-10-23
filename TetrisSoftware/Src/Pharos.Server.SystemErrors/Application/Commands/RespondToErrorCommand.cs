using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.SystemErrors.Application.Events;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;
using Tetris.Protocol.Common;

namespace Pharos.Server.SystemErrors.Application.Commands;

public record RespondToErrorCommand(int SystemErrorId, ResponseAction ResponseAction) : IRequest<DateTimeOffset>;

internal class RespondToErrorCommandHandler : IRequestHandler<RespondToErrorCommand, DateTimeOffset>
{
    private readonly ISystemErrorsDbContext _dbContext;
    private readonly ISender _sender;
    private readonly IDateTime _dateTime;
    private readonly ILogger<RespondToErrorCommandHandler> _logger;

    public RespondToErrorCommandHandler(ISystemErrorsDbContext dbContext, ISender sender, IDateTime dateTime, ILogger<RespondToErrorCommandHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<DateTimeOffset> Handle(RespondToErrorCommand request, CancellationToken cancellationToken)
    {
        // AutoResolve is system-triggered only and should never come from the client
        if (request.ResponseAction == ResponseAction.AutoResolve)
        {
            throw new ValidationException("AutoResolve cannot be triggered by a user. This is a system-triggered action only.");
        }

        var error = await _dbContext.SystemErrors.FirstOrDefaultAsync(x => x.Id == request.SystemErrorId, cancellationToken);
        if (error is null)
        {
            throw new NotFoundException(nameof(SystemError), request.SystemErrorId);
        }

        if (error.Response.HasValue)
        {
            throw new ValidationException($"Response to error with id '{request.SystemErrorId}' has already been sent. Response sent was '{error.Response}'");
        }

        if (!error.PossibleResponses.Contains(request.ResponseAction))
        {
            throw new ValidationException($"Error with id '{request.SystemErrorId}' does not contain error response {request.ResponseAction}");
        }

        // Update error with response.
        error.Response = request.ResponseAction;
        var responseTimestamp = _dateTime.Now;
        error.ResponseTimestamp = responseTimestamp;
        error.AddDomainEvent(new SystemErrorUpdated(error));

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Notify instrument for responses that should be sent to it.
        var shouldNotifyInstrument = request.ResponseAction != ResponseAction.Acknowledge
                                     && error.Source is ErrorSource.Instrument or ErrorSource.ProcessCore;
        if (shouldNotifyInstrument)
        {
            if (error.SerialNumber is null)
            {
                throw new ValidationException($"SerialNumber is required to send a response back to the instrument. ErrorId '{error.ErrorId}'");
            }

            try
            {
                await _sender.Send(new SendErrorResponseCommand(error.ErrorId, MapToGrpcResponse(request.ResponseAction), error.SerialNumber), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send error response for ErrorId {ErrorId} to instrument {SerialNumber}", error.ErrorId, error.SerialNumber);
            }
        }

        // Refresh task card state after response — remove the card if no unresolved errors remain,
        // otherwise recalculate the current highest severity.
        await SystemErrorTaskCardSync.RefreshErrorTaskCardsAsync(
            _dbContext,
            _sender,
            [error.SerialNumber],
            cancellationToken);

        return responseTimestamp;
    }

    private static EnumErrorResponseAction MapToGrpcResponse(ResponseAction responseAction)
    {
        return responseAction switch
        {
            ResponseAction.Acknowledge => throw new ArgumentException("Acknowledge should not be sent to instrument"),
            ResponseAction.AutoResolve => throw new ArgumentException("AutoResolve should not be sent to instrument"),
            ResponseAction.Abort => EnumErrorResponseAction.Abort,
            ResponseAction.Ignore => EnumErrorResponseAction.Ignore,
            ResponseAction.Retry => EnumErrorResponseAction.Retry,
            ResponseAction.Skip => EnumErrorResponseAction.Skip,
            ResponseAction.SkipAll => EnumErrorResponseAction.SkipAll,
            ResponseAction.Stop => EnumErrorResponseAction.Stop,
            _ => throw new ArgumentOutOfRangeException(nameof(responseAction), responseAction, null)
        };
    }
}