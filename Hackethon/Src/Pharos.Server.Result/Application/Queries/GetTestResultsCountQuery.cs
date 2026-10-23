using MediatR;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries;

internal record GetTestResultsCountQuery(string SerialNumber) : IRequest<int>;

internal sealed class GetTestResultsCountQueryHandler(IResultDbContext context) : IRequestHandler<GetTestResultsCountQuery, int>
{
    public async Task<int> Handle(GetTestResultsCountQuery request, CancellationToken cancellationToken)
    {
        var query = context.TestResults
            .Where(x=> x.InstrumentNumber == request.SerialNumber)
            .AsQueryable();

        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }
}