using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Events;

/// <summary>
/// Handles UpdateRequestorCountersCommand by updating the number of reported
/// samples and tests for each requestor.
/// </summary>
internal class UpdateRequestorCountersCommandHandler(
    IRequestDbContext dbContext) : IRequestHandler<UpdateRequestorCountersCommand>
{
    public async Task Handle(UpdateRequestorCountersCommand notification, CancellationToken cancellationToken)
    {
        var testOrderIds = notification.TestOrderIds;

        // Count reported test orders per requestor
        var testOrderStats = await dbContext.TestOrders
            .Where(x => testOrderIds.Contains(x.Uid)
                    && x.Request.RequestorId != null)
            .GroupBy(x => new { x.Request.RequestorId })
            .Select(g => new
            {
                g.Key.RequestorId,
                ReportedTests = g.Count()
            })
            .ToListAsync(cancellationToken);

        // Load only the relevant requestors — filter on primary key
        var requestorIds = testOrderStats
            .Select(x => x.RequestorId!.Value)
            .Distinct()
            .ToList();

        // If no requestors are found for test orders, exit early
        if (requestorIds.Count == 0)
        {
            return;
        }

        // Find requests that are now fully reported and not yet counted
        var newlySampledRequests = await dbContext.Requests
            .Where(r => r.RequestorId != null
                     && !r.IsCountedInRequestor
                     && r.TestOrders.Any(t => testOrderIds.Contains(t.Uid))
                     && r.TestOrders.All(t => t.State == TestOrderState.Reported))
            .ToListAsync(cancellationToken);

        // Mark as counted to avoid double counting
        newlySampledRequests.ForEach(r => r.IsCountedInRequestor = true);

        // Count new samples per requestor
        var sampleStats = newlySampledRequests
            .GroupBy(r => r.RequestorId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Count new tests per requestor
        var testStats = testOrderStats
            .ToDictionary(x => x.RequestorId!.Value, x => x.ReportedTests);

        // Load the requestors to update
        var requestors = await dbContext.Requestors
        .Where(r => requestorIds.Contains(r.Id))
        .ToListAsync(cancellationToken);

        foreach (var requestor in requestors)
        {
            // Update the counters for each requestor based on the newly counted samples and tests
            requestor.NumberOfReportedTests += testStats.GetValueOrDefault(requestor.Id);
            requestor.NumberOfReportedSamples += sampleStats.GetValueOrDefault(requestor.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}