using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Events;

internal class TubePriorityChangedEventHandler : INotificationHandler<TubePriorityChangedEvent>
{
    private readonly IRequestDbContext _dbContext;
    private readonly ISender _sender;

    public TubePriorityChangedEventHandler(IRequestDbContext dbContext, ISender sender)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Handle(TubePriorityChangedEvent notification, CancellationToken cancellationToken)
    {
        //Only possible to set it to high priority. Not removing the high priority flag.
        if (notification.HighPriority)
        {
            var requestId = await _sender.Send(new GetRequestIdQuery(notification.SampleTubeId.SampleId), cancellationToken);

            if (requestId.RequestExists)
            {
                var request = await _dbContext.Requests.Where(x => x.Id == requestId.RequestId)
                    .Include(x => x.TestOrders)
                    .ThenInclude(x => x.ReflexDefinitions)
                    .FirstOrDefaultAsync(cancellationToken);

                // only update if it was not already high priority
                if (request is { HighPriority: false })
                {
                    request.HighPriority = true;
                    request.AddDomainEvent(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()));
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}