using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.TaskCard.Application.Queries;

internal class DoesTaskCardExistQueryHandler : IRequestHandler<DoesTaskCardExistQuery, bool>
{
    private readonly ITaskCardService _service;

    public DoesTaskCardExistQueryHandler(ITaskCardService service)
    {
        _service = service;
    }

    public Task<bool> Handle(DoesTaskCardExistQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_service.Exists(request.CardType, request.InstrumentNumber));
    }
}