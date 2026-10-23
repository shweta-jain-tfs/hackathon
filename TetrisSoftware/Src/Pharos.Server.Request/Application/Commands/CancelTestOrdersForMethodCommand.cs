using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Request.Application.Interfaces;
using Tetris.Protocol.Instrument.TestOrder;

namespace Pharos.Server.Request.Application.Commands;

internal record CancelTestOrdersForMethodCommand(string MethodName, string? TestName = null) : IRequest;

internal sealed class CancelTestOrdersForMethodCommandHandler(
    IRequestDbContext dbContext,
    ISender sender,
    ILogger<CancelTestOrdersForMethodCommandHandler> logger) : IRequestHandler<CancelTestOrdersForMethodCommand>
{
    public async Task Handle(CancelTestOrdersForMethodCommand command, CancellationToken cancellationToken)
    {
        var query = dbContext.TestOrders
            .Where(t => t.MethodName == command.MethodName
                        && (t.State == TestOrderState.Scheduled || t.State == TestOrderState.UnableToStart));

        if (!string.IsNullOrEmpty(command.TestName))
        {
            query = query.Where(t => t.TestName == command.TestName);
        }

        var testOrders = await query
            .Where(t => !string.IsNullOrEmpty(t.InstrumentNumber))
            .Select(t => new { t.Uid, t.InstrumentNumber })
            .ToListAsync(cancellationToken);

        var testOrdersByInstrument = testOrders
            .GroupBy(t => t.InstrumentNumber!)
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
                await sender.Send(new DeleteTestOrderCommand(instrumentGroup.Key, deleteRequest), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e,
                    "Failed to cancel test orders for method {MethodName}, test {TestName} on instrument {Instrument}",
                    command.MethodName,
                    command.TestName,
                    instrumentGroup.Key);
            }
        }
    }
}
