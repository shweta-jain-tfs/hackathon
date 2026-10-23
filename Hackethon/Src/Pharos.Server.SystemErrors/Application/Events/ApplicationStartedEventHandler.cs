using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.SystemErrors.Application.Commands;
using Pharos.Server.SystemErrors.Application.Interfaces;

namespace Pharos.Server.SystemErrors.Application.Events;

internal class ApplicationStartedEventHandler(
    ISystemErrorsDbContext dbContext,
    ISender sender,
    ILogger<ApplicationStartedEventHandler> logger) : INotificationHandler<ApplicationStartedEvent>
{
    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Refreshing error task cards from unresolved system errors.");

        try
        {
            // Collect every distinct serial number that has at least one unresolved error,
            // plus null (global). RefreshErrorTaskCardsAsync handles each target.
            var serialNumbers = await dbContext.SystemErrors
                .Where(x => !x.Response.HasValue)
                .Select(x => x.SerialNumber)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Always include null so global errors are also evaluated.
            if (!serialNumbers.Contains(null))
            {
                serialNumbers.Add(null);
            }

            await SystemErrorTaskCardSync.RefreshErrorTaskCardsAsync(dbContext, sender, serialNumbers, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to refresh error task cards on startup.");
        }
    }
}
