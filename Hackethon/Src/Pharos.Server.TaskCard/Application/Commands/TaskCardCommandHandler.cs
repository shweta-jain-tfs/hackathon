using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure;
using Pharos.Server.TaskCard.Application.Interfaces;
using Pharos.Server.TaskCard.Domain;

namespace Pharos.Server.TaskCard.Application.Commands;

internal class TaskCardCommandHandler : IRequestHandler<TaskCardCommand>
{
    private readonly ITaskCardService _service;
    private readonly IDateTime _dateTime;

    public TaskCardCommandHandler(ITaskCardService service, IDateTime dateTime)
    {
        _service = service;
        _dateTime = dateTime;
    }

    public Task Handle(TaskCardCommand request, CancellationToken cancellationToken)
    {
        var taskCard = new Domain.TaskCard(request.Rules.Select(x => new TriggerState(x.CardState, _dateTime.Now.Add(x.Trigger))).ToList())
        {
            CardType = request.CardType,
            ExpiresAt = request.ExpiresAt.HasValue ? _dateTime.Now.Add(request.ExpiresAt.Value) : null,
            InstrumentNumber = request.InstrumentNumber
        };

        _service.Add(taskCard);
        return Task.CompletedTask;
    }
}