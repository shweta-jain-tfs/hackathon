using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.RequestIssue;

internal class RequestIssueEventHandler : INotificationHandler<TubesIdentifiedEvent>, INotificationHandler<TubesUnloadedEvent>, INotificationHandler<RequestUpdatedEvent>
{
    private readonly IRequestDbContext _dbContext;
    private readonly ISender _sender;
    private readonly IInstrumentManager _instrumentManager;
    private readonly NotReportedRequestIssueEvaluator _notReportedRequestIssueEvaluator;

    public RequestIssueEventHandler(
        IRequestDbContext dbContext,
        ISender sender,
        IInstrumentManager instrumentManager,
        NotReportedRequestIssueEvaluator notReportedRequestIssueEvaluator)
    {
        _dbContext = dbContext;
        _sender = sender;
        _instrumentManager = instrumentManager;
        _notReportedRequestIssueEvaluator = notReportedRequestIssueEvaluator;
    }

    //TODO, should we listen to when the tube becomes Partially Completed?
    //When this occurs we could suggest to move the tube to another instrument that can run the test order.

    public async Task Handle(TubesIdentifiedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var tube in notification.Tubes)
        {
            var request = await GetRequest(tube.Tube.SampleId, cancellationToken);
            if (request == null)
            {
                continue;
            }

            //Start removing RequestIssues

            //Tube Error - Assume the newly loaded tube don´t have errors
            await RemoveIssue(request.Id, RequestIssueType.TubeError, cancellationToken);

            //Tests Remaining - Since a tube is loaded
            await RemoveIssue(request.Id, RequestIssueType.TestsRemaining, cancellationToken);

            //Tests Remaining Load Tube - Since a tube is loaded
            await RemoveIssue(request.Id, RequestIssueType.TestsRemainingLoadTube, cancellationToken);
        }
    }

    public async Task Handle(TubesUnloadedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var unloadedTube in notification.Tubes)
        {
            var request = await GetRequest(unloadedTube.SampleId, cancellationToken);
            if (request != null)
            {
                var noTestOrders = request.TestOrders.Count == 0;

                if (HasTestOrdersPossibleToRun(request.TestOrders))
                {
                    if (unloadedTube.HasErrors)
                    {
                        //Tube Error
                        await CreateIssue(request.Id, RequestIssueType.TubeError, unloadedTube.TubeId, cancellationToken);
                    }

                    var allTubes = await GetSampleTubes(request.GetRequestUid(), cancellationToken);

                    if (AllTubesUnloaded(allTubes))
                    {
                        var instrumentNumber = await GetSuitableInstrument(request.TestOrders, cancellationToken);

                        if (!string.IsNullOrEmpty(instrumentNumber))
                        {
                            //if we want to load the tube to another instrument, we need to remove the Tests Remaining issue (if it exists)
                            await RemoveIssue(request.Id, RequestIssueType.TestsRemaining, cancellationToken);

                            //and create a Tests Remaining Load Tube issue instead.
                            await CreateIssue(request.Id, RequestIssueType.TestsRemainingLoadTube, instrumentNumber, cancellationToken);
                        }
                        else
                        {
                            //Tests Remaining
                            await CreateIssue(request.Id, RequestIssueType.TestsRemaining, cancellationToken);
                        }
                    }
                }
                else if (noTestOrders)
                {
                    //Missing Test Orders
                    await CreateIssue(request.Id, RequestIssueType.MissingTestOrders, cancellationToken);
                }
            }
        }
    }

    public async Task Handle(RequestUpdatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.TestOrders.Count > 0)
        {
            await RemoveIssue(notification.RequestId, RequestIssueType.MissingTestOrders, cancellationToken);
        }

        if (!HasTestOrdersPossibleToRun(notification.TestOrders))
        {
            // nothing more to run, remove tube error
            await RemoveIssue(notification.RequestId, RequestIssueType.TubeError, cancellationToken);

            // and no need to load tube anymore
            await RemoveIssue(notification.RequestId, RequestIssueType.TestsRemainingLoadTube, cancellationToken);

            // nothing more to run, remove tests remaining
            await RemoveIssue(notification.RequestId, RequestIssueType.TestsRemaining, cancellationToken);
        }

        await UpdateNotReportedIssue(notification, cancellationToken);
    }

    private async Task<string?> GetSuitableInstrument(IList<TestOrder> testOrders, CancellationToken cancellationToken)
    {
        var instruments = _instrumentManager.GetInstruments();

        foreach (var testOrder in testOrders.Where(TestOrdersPossibleToRun()).OrderBy(x => x.TestSortOrder))
        {
            foreach (var instrument in instruments)
            {
                if (await CanInstrumentRunTestOrder(instrument.SerialNumber, testOrder, cancellationToken))
                {
                    return instrument.DisplayName;
                }
            }
        }

        return null;
    }

    private async Task<bool> CanInstrumentRunTestOrder(string instrumentNumber, TestOrder testOrder, CancellationToken cancellationToken)
    {
        var enabledMethods = await _sender.Send(new GetActiveInstrumentMethodsQuery(instrumentNumber), cancellationToken);
        return enabledMethods.Contains(testOrder.MethodName);
    }

    private async Task CreateIssue(int requestId, RequestIssueType requestIssueType, string? key, CancellationToken cancellationToken)
    {
        await _sender.Send(new CreateRequestIssueCommand(requestId, requestIssueType, key), cancellationToken);
    }

    private async Task CreateIssue(int requestId, RequestIssueType requestIssueType, CancellationToken cancellationToken)
    {
        await CreateIssue(requestId, requestIssueType, null, cancellationToken);
    }

    private async Task RemoveIssue(int requestId, RequestIssueType requestIssueType, string? key, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteRequestIssueCommand(requestId, requestIssueType, key), cancellationToken);
    }

    private async Task RemoveIssue(int requestId, RequestIssueType requestIssueType, CancellationToken cancellationToken)
    {
        await RemoveIssue(requestId, requestIssueType, null, cancellationToken);
    }

    /// <summary>
    /// Adds or removes the "Report results" task after a request changes.
    /// </summary>
    private async Task UpdateNotReportedIssue(RequestUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var request = await GetRequest(notification.RequestId, cancellationToken);

        if (request != null && await _notReportedRequestIssueEvaluator.ShouldCreateAsync(request))
        {
            await CreateIssue(notification.RequestId, RequestIssueType.NotReported, cancellationToken);
            return;
        }

        await RemoveIssue(notification.RequestId, RequestIssueType.NotReported, cancellationToken);
    }

    private static bool HasTestOrdersPossibleToRun(IEnumerable<TestOrder> testOrders)
    {
        return testOrders.Any(ITestOrdersPossibleToRun());
    }

    private static Func<TestOrder, bool> TestOrdersPossibleToRun()
    {
        return ITestOrdersPossibleToRun();
    }

    private static bool HasTestOrdersPossibleToRun(IEnumerable<ITestOrder> testOrders)
    {
        return testOrders.Any(ITestOrdersPossibleToRun());
    }

    private static Func<ITestOrder, bool> ITestOrdersPossibleToRun()
    {
        return x => x is { IsActive: true, State: TestOrderState.NotStarted or TestOrderState.UnableToStart };
    }

    private static bool AllTubesUnloaded(List<CurrentSampleTubeDto> tubes)
    {
        //TODO, instrumentNumber will not be set to null in the future when the tube is unloaded. Need another way to determine if the tube is unloaded. For now, this will work.
        return tubes.Count == 0 || tubes.All(t => t.InstrumentNumber == null);
    }

    private async Task<List<CurrentSampleTubeDto>> GetSampleTubes(RequestUid requestUid, CancellationToken cancellationToken)
    {
        var items = await _sender.Send(new GetCurrentSampleTubesQuery([requestUid]), cancellationToken);
        var item = items.FirstOrDefault();

        if (item != null)
        {
            return item.Tubes;
        }

        return [];
    }

    private async Task<Domain.Request?> GetRequest(string sampleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRequestIdQuery(sampleId), cancellationToken);

        if (result.RequestExists)
        {
            return await GetRequest(result.RequestId, cancellationToken);
        }

        return null;
    }

    private async Task<Domain.Request?> GetRequest(int requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.Requests
            .Include(x => x.TestOrders)
            .Where(x => x.Id == requestId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
