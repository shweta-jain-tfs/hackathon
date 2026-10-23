using MediatR;
using Pharos.Server.Request.Application.Commands.ProcessLisExport;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Commands;

internal record ExportRequestsToLisCommand(List<int> RequestIds) : IRequest;

internal class ExportRequestsToLisCommandHandler : IRequestHandler<ExportRequestsToLisCommand>
{
    private readonly IRequestDbContext _dbContext;
    private readonly ISender _sender;

    public ExportRequestsToLisCommandHandler(IRequestDbContext dbContext, ISender sender)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Handle(ExportRequestsToLisCommand request, CancellationToken cancellationToken)
    {
        var requests = await _dbContext.Requests
            .Where(r => request.RequestIds.Contains(r.Id))
            .Include(r => r.TestOrders)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var testOrderUids = requests
            .SelectMany(r => r.TestOrders.Select(to => to.Uid))
            .ToList();

        if (testOrderUids.Count == 0)
        {
            return;
        }

        await _sender.Send(
            new ProcessLisExportCommand(
                Requests: requests,
                TestOrderUids: testOrderUids,
                IsManualExport: true),
            cancellationToken).ConfigureAwait(false);
    }
}
