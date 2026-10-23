using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.TaskCard.Application.Commands;

internal class RemoveTaskCardCommandHandler : IRequestHandler<RemoveTaskCardCommand>
{
    private readonly ITaskCardService _service;
    
    public RemoveTaskCardCommandHandler(ITaskCardService service)
    {
        _service = service;
    }

    public Task Handle(RemoveTaskCardCommand request, CancellationToken cancellationToken)
    {
        _service.Remove(request.CardType, request.InstrumentNumber);
        return Task.CompletedTask;
    }
}