using MediatR;
using Pharos.Server.Application.Sample;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries;

public record GetSampleTubeHistoryByIdQuery(int Id) : IRequest<List<SampleTubeHistoryDto>>;

internal class GetSampleTubeHistoryByIdQueryHandler(IRequestDbContext dbContext, ISender sender)
    : IRequestHandler<GetSampleTubeHistoryByIdQuery, List<SampleTubeHistoryDto>>
{
    public async Task<List<SampleTubeHistoryDto>> Handle(GetSampleTubeHistoryByIdQuery request, CancellationToken cancellationToken)
    {
        var requestUid = await dbContext.Requests
            .Where(x => x.Id == request.Id)
            .Select(x => new { x.SampleId, x.Created })
            .FirstOrDefaultAsync(cancellationToken);

        if (requestUid == null)
        {
            return new List<SampleTubeHistoryDto>();
        }

        return await sender.Send(new GetSampleTubeHistoryQuery(requestUid.SampleId, requestUid.Created), cancellationToken);
    }
}