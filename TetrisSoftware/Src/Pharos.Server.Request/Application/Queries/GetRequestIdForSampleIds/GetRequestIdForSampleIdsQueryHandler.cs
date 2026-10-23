using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries.GetRequestIdForSampleIds;

internal class GetRequestIdForSampleIdsQueryHandler(
    IRequestDbContext dbContext,
    ISender sender) : IRequestHandler<GetRequestIdForSampleIdsQuery, IReadOnlyDictionary<(string SampleId, DateTimeOffset RequestCreatedDate), string>>
{
    public async Task<IReadOnlyDictionary<(string SampleId, DateTimeOffset RequestCreatedDate), string>> Handle(GetRequestIdForSampleIdsQuery request, CancellationToken cancellationToken)
    {
        var dictionary = new Dictionary<(string SampleId, DateTimeOffset RequestCreatedDate), string>();

        foreach (var item in request.RequestUidItems)
        {
            var requestMatch = await sender.Send(
                new GetRequestIdQuery(item.SampleId, item.RequestCreatedDate),
                cancellationToken);

            var requestId = requestMatch.RequestExists
                ? await dbContext.Requests
                    .Where(x => x.Id == requestMatch.RequestId)
                    .Select(x => x.RequestId)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false)
                : null;

            dictionary.Add((item.SampleId, item.RequestCreatedDate), requestId ?? string.Empty);
        }

        return dictionary;
    }
}
