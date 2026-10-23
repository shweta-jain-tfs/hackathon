using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Commands.RequestIssue;
using Pharos.Server.Request.Application.Interfaces;
using RequestDeletedEvent = Pharos.Server.Application.Request.RequestDeletedEvent;

namespace Pharos.Server.Request.Application.Jobs;

internal class CleanupRequestsCommandHandler : IRequestHandler<CleanupRequestsCommand>
{
    private readonly IDateTime _dateTime;
    private readonly IRequestDbContext _dbContext;
    private readonly ILogger<CleanupRequestsCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IRequestHub _requestHub;

    public CleanupRequestsCommandHandler(ILogger<CleanupRequestsCommandHandler> logger, IRequestDbContext dbContext, IDateTime dateTime, IRequestHub requestHub, IMediator mediator)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _requestHub = requestHub;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(CleanupRequestsCommand request, CancellationToken cancellationToken)
    {
        var compareDate = _dateTime.Now.Subtract(TimeSpan.FromDays(request.OlderThanDays));

        _logger.LogInformation("Deleting requests that has not been modified since {CompareDate} ", compareDate);

        // Find old requests
        var oldRequestIds = await _dbContext.Requests
            .Where(r => r.LastModified < compareDate)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        // Filter to only include requests where all test orders are Reported (or no test orders exist)
        var requestsWithNonReportedOrders = await _dbContext.TestOrders
            .Where(to => oldRequestIds.Contains(to.RequestId) && to.State != TestOrderState.Reported)
            .Select(to => to.RequestId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Exclude requests that have non-reported test orders
        var requestIdsToDelete = oldRequestIds.Except(requestsWithNonReportedOrders).ToList();

        // Get full request records for the ones we will delete
        var requestRecords = await _dbContext.Requests
            .Where(r => requestIdsToDelete.Contains(r.Id))
            .Select(r => new { r.Id, r.SampleId, r.Created })
            .ToListAsync(cancellationToken);

        var requestIds = requestRecords.Select(x => x.Id).ToList();

        var testOrderIds = await _dbContext.TestOrders.Where(x => requestIds.Contains(x.RequestId)).Select(x => x.Uid).ToListAsync(cancellationToken);

        var numberOfDeletedRequests = await _dbContext.Requests.Where(r => requestIds.Contains(r.Id)).ExecuteDeleteAsync(cancellationToken);
        _logger.LogInformation("Deleted {NumberOfDeletedRequests} requests", numberOfDeletedRequests);

        var numberOfDeletedPatients = await _dbContext.Patients.Include(x => x.Requests).Where(p => p.Requests == null || (p.Requests != null && p.Requests.Count == 0)).ExecuteDeleteAsync(cancellationToken);
        _logger.LogInformation("Deleted {NumberOfDeletedPatients} orphan patients", numberOfDeletedPatients);

        // Notify clients that requests have been deleted
        if (numberOfDeletedRequests > 0)
        {
            await _requestHub.RequestsDeleted(requestIds);
        }

        await _mediator.Publish(new TestOrdersDeletedEvent(testOrderIds), cancellationToken);

        foreach (var record in requestRecords)
        {
            await _mediator.Publish(new RequestDeletedEvent(new RequestUid { SampleId = record.SampleId, RequestCreatedDate = record.Created }), cancellationToken);
        }

        await _mediator.Send(new RegenerateRequestIssueTaskCardsCommand(), cancellationToken);
    }
}