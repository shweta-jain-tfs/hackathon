using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.SystemErrors.Application.Helpers;
using Pharos.Server.SystemErrors.Application.Interfaces;

namespace Pharos.Server.SystemErrors.Application.Commands;

internal static class SystemErrorTaskCardSync
{
    /// <summary>
    /// Refreshes error task cards from unresolved system errors.
    /// Recalculates instrument cards for the provided serial numbers and always refreshes the global card.
    /// Cards are updated when unresolved errors exist, otherwise removed.
    /// </summary>
    public static async Task RefreshErrorTaskCardsAsync(
        ISystemErrorsDbContext dbContext,
        ISender sender,
        IEnumerable<string?> serialNumbers,
        CancellationToken cancellationToken)
    {
        // Refresh instrument error task cards.
        foreach (var serialNumber in serialNumbers.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        {
            var classifications = await dbContext.SystemErrors
                .Where(x => x.SerialNumber == serialNumber && !x.Response.HasValue)
                .Select(x => x.Classification)
                .ToListAsync(cancellationToken);

            await UpdateOrRemoveTaskCardAsync(sender, classifications, TaskCardType.Error, serialNumber, cancellationToken);
        }

        // Always refresh the global card (global and instrument cards are shown together).
        var globalClassifications = await dbContext.SystemErrors
            .Where(x => x.SerialNumber == null && !x.Response.HasValue)
            .Select(x => x.Classification)
            .ToListAsync(cancellationToken);

        await UpdateOrRemoveTaskCardAsync(sender, globalClassifications, TaskCardType.SystemError, null, cancellationToken);
    }

    /// <summary>
    /// Applies task card changes for one target (instrument or global).
    /// If there are no unresolved error classifications, the task card is removed.
    /// Otherwise, the task card is updated with the highest-severity state.
    /// </summary>
    private static async Task UpdateOrRemoveTaskCardAsync(
        ISender sender,
        List<ErrorClassification> classifications,
        TaskCardType cardType,
        string? serialNumber,
        CancellationToken cancellationToken)
    {
        if (classifications.Count == 0)
        {
            await sender.Send(new RemoveTaskCardCommand(cardType, serialNumber), cancellationToken);
        }
        else
        {
            // Recalculate and apply the current highest-severity state from unresolved errors.
            var state = ErrorClassificationMapper.GetHighestTaskCardState(classifications);
            await sender.Send(new TaskCardCommand(cardType, state, serialNumber), cancellationToken);
        }
    }
}
