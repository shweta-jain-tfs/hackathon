using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Request.Application.Commands.DeleteRequest;
using Pharos.Server.Request.Application.Interfaces;
using EntityState = Pharos.Server.Application.Request.EntityState;

namespace Pharos.Server.Request.Application.Events;

/// <summary>
/// Handles SampleTestResultsSentToLisEvent by marking test orders as Reported.
/// This handler is invoked AFTER the message is successfully sent to LIS.
/// Applies to ALL export types (manual and periodic).
/// </summary>
internal class SampleTestResultsSentToLisEventHandler(
    IRequestDbContext dbContext,
    IOptionsSnapshot<LisSettings> lisSettings,
    IMediator mediator) : INotificationHandler<SampleTestResultsSentToLisEvent>
{
    public async Task Handle(SampleTestResultsSentToLisEvent notification, CancellationToken cancellationToken)
    {
        // Query test orders by GUIDs
        var testOrders = await dbContext.TestOrders
            .Where(x => notification.TestOrderIds.Contains(x.Uid))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (testOrders.Count == 0)
        {
            return;
        }

        // Get all unique request IDs for the test orders
        var requestIds = testOrders
            .Select(x => x.RequestId)
            .Distinct()
            .ToList();

        // Load requests with their test orders for state updates
        var requests = await dbContext.Requests
            .Where(x => requestIds.Contains(x.Id))
            .Include(x => x.TestOrders)
            .ThenInclude(x => x.ReflexDefinitions)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var testOrderIds = notification.TestOrderIds.ToHashSet();

        // Update test order states
        var updatedRequests = new HashSet<Domain.Request>(); 
        var reportedTestOrderIds = new List<Guid>();
        foreach (var request in requests)
        {
            foreach (var test in request.TestOrders)
            {
                if (testOrderIds.Contains(test.Uid) &&
                    (test.State is TestOrderState.Rejected or TestOrderState.Approved))
                {
                    test.State = TestOrderState.Reported;
                    test.EntityState = EntityState.Modified;
                    updatedRequests.Add(request);
                    reportedTestOrderIds.Add(test.Uid);
                }
            }

            // Update request state based on test order states
            request.UpdateRequestState();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var request in updatedRequests)
        {
            await mediator.Publish(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()), cancellationToken);
        }

        await mediator.Send(new UpdateRequestorCountersCommand(reportedTestOrderIds), cancellationToken);

        // Delete samples after export if setting is enabled
        await DeleteSamplesAfterExportIfEnabled(notification.LisChannelId, requests, cancellationToken);
    }

    /// <summary>
    /// Deletes samples (requests) after successful LIS export if the setting is enabled.
    /// Only deletes samples where ALL test orders have been reported.
    /// </summary>
    private async Task DeleteSamplesAfterExportIfEnabled(
        int lisChannelId,
        List<Domain.Request> requests,
        CancellationToken cancellationToken)
    {
        if (!ShouldDeleteSamplesAfterExport(lisChannelId))
        {
            return;
        }

        var requestIdsToDelete = requests
            .Where(r => r.TestOrders != null &&
                        r.TestOrders.Count > 0 &&
                        r.TestOrders.All(t => t.State == TestOrderState.Reported))
            .Select(r => r.Id)
            .ToArray();

        if (requestIdsToDelete.Length == 0)
        {
            return;
        }

        await mediator.Send(new DeleteRequestCommand(requestIdsToDelete), cancellationToken);
    }

    private bool ShouldDeleteSamplesAfterExport(int lisChannelId)
    {
        var channels = lisSettings.Value.LisChannels;

        if (lisChannelId >= channels.Count || lisChannelId < 0)
        {
            return false;
        }

        var channel = channels[lisChannelId];
        var sampleSettings = channel.ExportSettings?.Sample;

        return sampleSettings?.DeleteSamplesAfterExport ?? false;
    }
}