using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries;

internal class GetTestOrderStatesQueryHandler(
    IRequestDbContext dbContext,
    ISender sender) : IRequestHandler<GetTestOrderStatesQuery, IReadOnlyDictionary<string, GetTestOrderStatesQueryResultItem>>
{
    public async Task<IReadOnlyDictionary<string, GetTestOrderStatesQueryResultItem>> Handle(GetTestOrderStatesQuery request, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, GetTestOrderStatesQueryResultItem>();

        foreach (var sampleId in request.SampleIds)
        {
            var requestIdResponse = await sender.Send(new GetRequestIdQuery(sampleId), cancellationToken);
            if (!requestIdResponse.RequestExists)
            {
                continue;
            }

            var item = await dbContext.Requests.Where(x => x.Id == requestIdResponse.RequestId)
                .Include(x => x.TestOrders)
                .ThenInclude(x => x.ReflexDefinitions)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            var requestUid = new RequestUid
            {
                SampleId = item.SampleId,
                RequestCreatedDate = item.Created
            };

            if (item.TestOrders is { Count: > 0 })
            {
                result.Add(requestUid.SampleId, new GetTestOrderStatesQueryResultItem(requestUid, new List<ITestOrder>(item.TestOrders)));
            }
            else
            {
                result.Add(requestUid.SampleId, new GetTestOrderStatesQueryResultItem(requestUid, []));
            }
        }

        return result;
    }
}