using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Helpers;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Commands.ResetTestOrders;

public record ResetTestOrdersCommand(IReadOnlyList<int> RequestIds, bool IncludeAbortedTests) : IRequest
{
    internal class Handler(IRequestDbContext dbContext) : IRequestHandler<ResetTestOrdersCommand>
    {
        public async Task Handle(ResetTestOrdersCommand request, CancellationToken cancellationToken)
        {
            var requests = await dbContext.Requests
                .Where(x => request.RequestIds.Contains(x.Id))
                .Include(x => x.TestOrders)
                    .ThenInclude(x => x.TestOrderCustomParameters)
                .Include(x => x.TestOrders)
                    .ThenInclude(x => x.ReflexDefinitions!)
                        .ThenInclude(x => x.ReflexTests!)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var req in requests)
            {
                var hasChanges = false;
                var testOrdersToReset = req.TestOrders
                    .Where(testOrder => TestOrderResetHelper.CanReset(testOrder.State, request.IncludeAbortedTests))
                    .ToList();

                foreach (var testOrder in testOrdersToReset)
                {
                    // Clone reflex definitions before removing the old test order to preserve them
                    var clonedReflexDefinitions = TestOrderResetHelper.CloneReflexDefinitions(testOrder.ReflexDefinitions);

                    // Remove the old test order from database and request
                    dbContext.TestOrders.Remove(testOrder);
                    req.TestOrders.Remove(testOrder);

                    // Create new test order with fresh UID, preserving all old values including sort order
                    var resetTestOrder = TestOrderResetHelper.ResetTestOrder(testOrder);

                    // Attach the cloned reflex definitions to the new test order
                    resetTestOrder.ReflexDefinitions = clonedReflexDefinitions;

                    req.TestOrders.Add(resetTestOrder);

                    hasChanges = true;
                }

                if (hasChanges)
                {
                    req.UpdateRequestState();
                    req.AddDomainEvent(new RequestUpdatedEvent(req.Id, req.GetRequestUid(), req.TestOrders.ToList()));
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
