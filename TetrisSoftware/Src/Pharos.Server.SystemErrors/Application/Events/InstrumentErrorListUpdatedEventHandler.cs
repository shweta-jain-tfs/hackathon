using FluentValidation;
using Google.Protobuf.Collections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.SystemErrors.Application.Commands;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Errors;

namespace Pharos.Server.SystemErrors.Application.Events;

internal class InstrumentErrorListUpdatedEventHandler(ISystemErrorsDbContext dbContext, InstrumentErrorListUpdatedEventValidator validator, ISender sender, ICurrentInstrumentServerHub currentInstrument, IInstrumentManager instrumentManager, IDateTime dateTime) : INotificationHandler<InstrumentErrorListUpdatedEvent>
{
    public async Task Handle(InstrumentErrorListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await ValidateErrors(notification, cancellationToken);

        var systemErrors = new List<SystemError>();
        var incomingIds = new HashSet<Guid>(notification.Message.Errors.Select(e => Guid.Parse(e.ErrorId)));

        var existingErrors = await dbContext.SystemErrors
            .Where(x => x.SerialNumber == notification.InstrumentNumber && incomingIds.Contains(x.ErrorId))
            .ToListAsync(cancellationToken);

        var hasNewFatalError = await HandleMergeAsync(notification, existingErrors, systemErrors, cancellationToken);
        await MarkMissingErrorsAsResolvedAsync(notification, incomingIds, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (hasNewFatalError)
        {
            await RemoveFatalTaskCardBeforeRefreshAsync(notification.InstrumentNumber, cancellationToken);
        }

        await SendTaskCardInformation(notification, cancellationToken);
        await SendFatalErrors(notification, systemErrors);
    }

    private async Task ValidateErrors(InstrumentErrorListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(notification, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("One or more validation failures have occurred.", validationResult.Errors);
        }
    }

    private async Task<bool> HandleMergeAsync(
        InstrumentErrorListUpdatedEvent notification,
        List<SystemError> existingErrors,
        List<SystemError> output,
        CancellationToken cancellationToken)
    {
        var byId = existingErrors.ToDictionary(e => e.ErrorId);
        var hasNewFatalError = false;

        foreach (var msg in notification.Message.Errors)
        {
            var id = Guid.Parse(msg.ErrorId); // safe after validation

            if (!byId.TryGetValue(id, out var existing))
            {
                var created = MapErrorMessage(msg, notification.InstrumentNumber);
                created.AddDomainEvent(new SystemErrorCreated(created));
                await dbContext.SystemErrors.AddAsync(created, cancellationToken);
                output.Add(created);

                if (created.Classification == ErrorClassification.Fatal)
                {
                    hasNewFatalError = true;
                }
            }
            else
            {
                if (existing.Response.HasValue)
                {
                    throw new NotAllowedException("Errors that have already been responded to must not be sent again");
                }

                // Update classification if it differs from the notification
                var newClassification = MapClassification(msg.Classification);
                if (existing.Classification != newClassification)
                {
                    existing.Classification = newClassification;
                    existing.AddDomainEvent(new SystemErrorUpdated(existing));

                    if (newClassification == ErrorClassification.Fatal)
                    {
                        hasNewFatalError = true;
                    }
                }

                output.Add(existing);
            }
        }

        return hasNewFatalError;
    }

    private async Task MarkMissingErrorsAsResolvedAsync(InstrumentErrorListUpdatedEvent notification, HashSet<Guid> idsReceived, CancellationToken cancellationToken)
    {
        // Errors not in the list are treated as resolved by setting AutoResolve response
        var autoResolvedErrors = await dbContext.SystemErrors.Where(x => x.SerialNumber == notification.InstrumentNumber && !x.Response.HasValue && !idsReceived.Contains(x.ErrorId)).ToListAsync(cancellationToken);

        foreach (var instrumentError in autoResolvedErrors)
        {
            instrumentError.Response = ResponseAction.AutoResolve;
            instrumentError.ResponseTimestamp = dateTime.Now;
            instrumentError.AddDomainEvent(new SystemErrorUpdated(instrumentError));
        }
    }

    private async Task SendTaskCardInformation(InstrumentErrorListUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // Use RefreshErrorTaskCardsAsync so the task card reflects the highest severity across
        // ALL unresponded errors (instrument-reported + Pharos), not just the instrument list.
        await SystemErrorTaskCardSync.RefreshErrorTaskCardsAsync(
            dbContext,
            sender,
            [notification.InstrumentNumber],
            cancellationToken);
    }

    private async Task SendFatalErrors(InstrumentErrorListUpdatedEvent notification, List<SystemError> systemErrors)
    {
        var instrumentId = instrumentManager.GetInstrument(notification.InstrumentNumber).Id;
        var fatalErrorIds = systemErrors
            .Where(x => x.Classification == ErrorClassification.Fatal)
            .Select(x => x.Id)
            .ToList();

        await currentInstrument.InstrumentFatalErrorChanged(instrumentId, fatalErrorIds);

    }

    private static SystemError MapErrorMessage(Error messageError, string instrumentNumber)
    {
        return new SystemError
        {
            SerialNumber = instrumentNumber,
            Code = messageError.Code,
            Classification = MapClassification(messageError.Classification),
            ErrorId = Guid.Parse(messageError.ErrorId),
            Count = messageError.Count,
            Timestamp = messageError.Timestamp.ToDateTimeOffset().ToLocalTime(),
            Source = MapSource(messageError.Source),
            PossibleResponses = MapPossibleResponses(messageError.PossibleResponses),
            Parameters = messageError.Parameters.Select(x => x.Value).ToList()
        };
    }

    private static ErrorClassification MapClassification(EnumErrorClassification classification)
    {
        return classification switch
        {
            EnumErrorClassification.Fatal => ErrorClassification.Fatal,
            EnumErrorClassification.Error => ErrorClassification.Error,
            EnumErrorClassification.Warning => ErrorClassification.Warning,
            EnumErrorClassification.Information => ErrorClassification.Information,
            _ => throw new ArgumentOutOfRangeException(nameof(classification), classification, null)
        };
    }

    private static List<ResponseAction> MapPossibleResponses(RepeatedField<EnumErrorResponseAction> possibleResponses)
    {
        // If the instrument did not send any possible responses, default to Confirm
        if (possibleResponses == null || possibleResponses.Count == 0)
        {
            return new List<ResponseAction> { ResponseAction.Acknowledge };
        }

        var mapped = possibleResponses
            .Where(x => x != EnumErrorResponseAction.UnspecifiedErrorResponseAction)
            .Select(MapPossibleResponse)
            .ToList();

        return mapped.Count == 0
            ? new List<ResponseAction> { ResponseAction.Acknowledge }
            : mapped;
    }

    private static ResponseAction MapPossibleResponse(EnumErrorResponseAction possibleResponse)
    {
        return possibleResponse switch
        {
            EnumErrorResponseAction.Abort => ResponseAction.Abort,
            EnumErrorResponseAction.Ignore => ResponseAction.Ignore,
            EnumErrorResponseAction.Retry => ResponseAction.Retry,
            EnumErrorResponseAction.Skip => ResponseAction.Skip,
            EnumErrorResponseAction.SkipAll => ResponseAction.SkipAll,
            EnumErrorResponseAction.Stop => ResponseAction.Stop,
            _ => throw new ArgumentOutOfRangeException(nameof(possibleResponse), possibleResponse, null)
        };
    }

    private static ErrorSource MapSource(EnumErrorSource messageErrorSource)
    {
        return messageErrorSource switch
        {
            EnumErrorSource.Instrument => ErrorSource.Instrument,
            EnumErrorSource.ProcessCore => ErrorSource.ProcessCore,
            _ => throw new ArgumentOutOfRangeException(nameof(messageErrorSource), messageErrorSource, null)
        };
    }

    /// <summary>
    /// Removes the current instrument fatal task card before refresh so a newly created fatal error re-adds and retriggers sound control.
    /// </summary>
    /// <param name="instrumentNumber">The instrument serial number.</param>
    /// <param name="cancellationToken">Cancellation token for command dispatch.</param>
    private async Task RemoveFatalTaskCardBeforeRefreshAsync(string instrumentNumber, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveTaskCardCommand(TaskCardType.Error, instrumentNumber), cancellationToken);
    }
}

internal class InstrumentErrorListUpdatedEventValidator : AbstractValidator<InstrumentErrorListUpdatedEvent>
{
    public InstrumentErrorListUpdatedEventValidator()
    {
        RuleFor(x => x.Message.Errors).Unique(x => x.ErrorId);
        RuleForEach(x => x.Message.Errors).SetValidator(new ErrorValidator());
    }
}

internal class ErrorValidator : AbstractValidator<Error>
{
    public ErrorValidator()
    {
        RuleFor(x => x.Code).GreaterThan(0);
        RuleFor(x => x.Classification).Must(x => x != EnumErrorClassification.UnspecifiedErrorClassification);
        RuleFor(x => x.Source).Must(x => x != EnumErrorSource.UnspecifiedErrorSource);
    }
}