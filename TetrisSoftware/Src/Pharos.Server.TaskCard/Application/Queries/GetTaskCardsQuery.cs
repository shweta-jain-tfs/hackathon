using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.TaskCard.Application.Queries;

internal record GetTaskCardsQuery(string InstrumentNumber) : IRequest<List<TaskCardDto>>;
internal record GetTaskCardsForLockScreenQuery(string InstrumentNumber) : IRequest<List<TaskCardDto>>;
internal record GetTaskCardInformationQuery(string InstrumentNumber) : IRequest<TaskCardInformationDto>;

internal sealed class GetTaskCardsQueryHandler : IRequestHandler<GetTaskCardsQuery, List<TaskCardDto>>, IRequestHandler<GetTaskCardsForLockScreenQuery, List<TaskCardDto>>, IRequestHandler<GetTaskCardInformationQuery, TaskCardInformationDto>
{
    private readonly ITaskCardService _service;

    public GetTaskCardsQueryHandler(ITaskCardService service)
    {
        _service = service;
    }

    public Task<List<TaskCardDto>> Handle(GetTaskCardsQuery request, CancellationToken cancellationToken)
    {
        var taskCards = _service.GetTaskCardsForInstrument(request.InstrumentNumber);
        return Task.FromResult(taskCards.Select(Map).ToList());
    }

    public Task<List<TaskCardDto>> Handle(GetTaskCardsForLockScreenQuery request, CancellationToken cancellationToken)
    {
        var taskCards = _service.GetTaskCardsForInstrumentLockScreen(request.InstrumentNumber);
        return Task.FromResult(taskCards.Select(Map).ToList());
    }

    public Task<TaskCardInformationDto> Handle(GetTaskCardInformationQuery request, CancellationToken cancellationToken)
    {
        var taskCards = _service.GetTaskCardsForInstrument(request.InstrumentNumber);

        if (taskCards.Count == 0)
        {
            return Task.FromResult(new TaskCardInformationDto());
        }

        var taskCardInfo = new TaskCardInformationDto
        {
            NoOfTaskCards = taskCards.Count,
            HighestTaskCardState = taskCards.Max(x => x.CurrentCardState)
        };

        return Task.FromResult(taskCardInfo);
    }

    private static TaskCardDto Map(Domain.TaskCard taskCard)
    {
        return new TaskCardDto
        {
            ExpiresAt = taskCard.ExpiresAt,
            CardType = taskCard.CardType == TaskCardType.SystemError ? TaskCardType.Error : taskCard.CardType,
            CardState = taskCard.CurrentCardState
        };
    }
}