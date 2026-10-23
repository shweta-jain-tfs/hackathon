using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Common;
using Pharos.Server.Request.Application.Commands.RequestIssue;
using Tetris.Protocol.Instrument.TestOrder;

namespace Pharos.Server.Request.Application.Events;

internal class RequestDeletedEvent : EntityDeletedEvent<Domain.Request>
{
    public RequestDeletedEvent(Domain.Request entity) : base(entity)
    {
    }
}

internal sealed class RequestDeletedEventHandler(IRequestHub hub, IMediator mediator, ILogger<RequestDeletedEventHandler> logger) : INotificationHandler<RequestDeletedEvent>
{
    public async Task Handle(RequestDeletedEvent notification, CancellationToken cancellationToken)
    {
        var deletedRequest = notification.DeletedEntity;

        await hub.Deleted(deletedRequest.Id);

        // Group test orders by instrument number and send delete commands
        var testOrdersByInstrument = deletedRequest.TestOrders.Where(x => x.State is TestOrderState.Scheduled or TestOrderState.UnableToStart)
            .GroupBy(x => x.InstrumentNumber!)
            .ToList();

        foreach (var instrumentGroup in testOrdersByInstrument)
        {
            var deleteRequest = new DeleteTestOrderRequest();
            foreach (var testOrder in instrumentGroup)
            {
                deleteRequest.TestOrderIds.Add(testOrder.Uid.ToString());
            }

            try
            {
                await mediator.Send(new DeleteTestOrderCommand(instrumentGroup.Key, deleteRequest), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to cancel test orders for instrument {Instrument}", instrumentGroup.Key);
            }
        }

        await mediator.Publish(new Pharos.Server.Application.Request.RequestDeletedEvent(deletedRequest.GetRequestUid()), cancellationToken);

        await mediator.Send(new RegenerateRequestIssueTaskCardsCommand(), cancellationToken);
    }
}