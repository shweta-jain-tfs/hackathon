using FluentValidation;
using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Jobs;
using Quartz;
using Tetris.Protocol.Pharos;
using EntityState = Pharos.Server.Application.Request.EntityState;
using TestOrderState = Pharos.Server.Domain.Enums.TestOrderState;

namespace Pharos.Server.Request.Application.Events;

internal class TestOrderStatusChangedEventHandler(
    IRequestDbContext dbContext,
    IMediator mediator,
    ISchedulerFactory schedulerFactory)
    : IRequestHandler<Server.Application.Instrument.TestOrderStatusChangedEvent>
{
    private static readonly List<TestOrderState> _isActiveHasToBeTrueStates =
    [
        TestOrderState.Started,
        TestOrderState.Pipetted,
        TestOrderState.PendingBlank,
        TestOrderState.PendingCalibration,
        TestOrderState.PendingApproval,
        TestOrderState.Flagged,
        TestOrderState.Aborted,
        TestOrderState.Rejected,
        TestOrderState.Approved,
        TestOrderState.ApprovedPendingReflex,
        TestOrderState.Reported
    ];

    public async Task Handle(Server.Application.Instrument.TestOrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var statusData = notification.Message.TestOrderStatus;
        var testOrderUid = Guid.Parse(statusData.TestOrderId);

        var requestId = await dbContext.TestOrders
            .Where(sto => sto.Uid == testOrderUid)
            .Select(sto => sto.RequestId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (requestId == 0)
        {
            // the test order have been deleted from the request, do nothing
            return;
        }

        var request = await dbContext.Requests
            .Where(x => x.Id == requestId)
            .Include(x => x.TestOrders)
            .ThenInclude(x => x.ReflexDefinitions)
            .Include(x => x.TestOrders)
            .ThenInclude(x => x.TestOrderCustomParameters)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        var testOrder = request.TestOrders.FirstOrDefault(x => x.Uid == testOrderUid);
        if (testOrder == null)
        {
            // race condition: the test order have been deleted from the request, do nothing
            return;
        }

        var status = statusData.Status;
        var newState = status switch
        {
            EnumTestOrderStatus.Started => TestOrderState.Started,
            EnumTestOrderStatus.Pipetted => TestOrderState.Pipetted,
            EnumTestOrderStatus.Scheduled => TestOrderState.Scheduled,
            EnumTestOrderStatus.SampleUnloaded or EnumTestOrderStatus.MethodNotActive or EnumTestOrderStatus.Cancelled => TestOrderState.NotStarted,
            EnumTestOrderStatus.UnableToStart => TestOrderState.UnableToStart,
            _ => throw new ValidationException($"Unexpected TestOrderStatus value: {status}")
        };

        if (newState == testOrder.State)
        {
            // nothing to update
            return;
        }

        testOrder.State = newState;
        testOrder.EntityState = EntityState.Modified;
        if (newState == TestOrderState.NotStarted)
        {
            testOrder.TubeId = null;
            testOrder.InstrumentNumber = null;
        }

        if (!testOrder.IsActive && _isActiveHasToBeTrueStates.Contains(newState))
        {
            testOrder.IsActive = true;
        }

        request.UpdateRequestState();

        await dbContext.SaveChangesAsync(cancellationToken);
        await mediator.Publish(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()), cancellationToken);

        if (newState == TestOrderState.NotStarted)
        {
            // we trigger a job with delay to try to send the not started test orders to instruments, because the instrument
            // may send a batch of cancelled/unloaded test order status updates, and we only want to re-schedule the test orders
            // after all the status updates have been received.
            await schedulerFactory.TriggerTrySendAllNotStartedTestsToInstrumentsJobWithDelay(cancellationToken);
        }
    }
}