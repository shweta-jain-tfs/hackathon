using MediatR;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries.GetRequestor;

public class GetRequestorByRequestIdQuery : IRequest<RequestorDto?>
{
    public string RequestorId { get; set; } = string.Empty;
}

internal class GetRequestorByRequestIdQueryHandler : IRequestHandler<GetRequestorByRequestIdQuery, RequestorDto?>
{
    private readonly IRequestDbContext _context;

    public GetRequestorByRequestIdQueryHandler(IRequestDbContext context)
    {
        _context = context;
    }

    public async Task<RequestorDto?> Handle(GetRequestorByRequestIdQuery request, CancellationToken cancellationToken)
    {
        var requestor = await _context.Requestors
            .Where(r => r.RequestorId == request.RequestorId)
            .Select(r => new RequestorDto
            {
                Id = r.Id,
                RequestorId = r.RequestorId,
                Name = r.Name,
                ReflexEnabled = r.ReflexEnabled
            })
            .FirstOrDefaultAsync(cancellationToken);

        return requestor;
    }
}