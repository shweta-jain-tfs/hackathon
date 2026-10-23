using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;

namespace Pharos.Server.LoadList.Application.Queries;

internal record GetReagentLoadersCountQuery(IEnumerable<string>? InstrumentNumbers = null, IEnumerable<LoadListStatus>? Statuses = null) : IRequest<int>;

internal class GetReagentLoadersCountQueryHandler(ILoadListService loadListService) : IRequestHandler<GetReagentLoadersCountQuery, int>
{
    public Task<int> Handle(GetReagentLoadersCountQuery request, CancellationToken cancellationToken)
    {
        var count = loadListService.GetReagentLoadersCount(request.InstrumentNumbers, request.Statuses);
        return Task.FromResult(count);
    }
}
