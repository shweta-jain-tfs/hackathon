using MediatR;
using Pharos.Server.Application.Request;

namespace Pharos.Server.Request.Application.Queries;
internal class GetRequestIdFromResultQueryHandler(ISender sender) : IRequestHandler<GetRequestIdFromResultQuery, RequestIdResponseDto>
{
    public Task<RequestIdResponseDto> Handle(GetRequestIdFromResultQuery request, CancellationToken cancellationToken)
    {
        return sender.Send(
            new GetRequestIdQuery(request.RequestUid.SampleId, request.RequestUid.RequestCreatedDate),
            cancellationToken);
    }
}
