using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Queries;
using Pharos.Server.TaskCard.Application.Queries;

namespace Pharos.Server.TaskCard.Web.Controllers;

[ApiController]
[Route("api/tasks")]
public class TaskCardController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentInstrumentService _currentInstrumentService;

    public TaskCardController(ISender sender, ICurrentInstrumentService currentInstrumentService)
    {
        _sender = sender;
        _currentInstrumentService = currentInstrumentService;
    }

    [HttpGet("all_instruments")]
    [Authorize]
    public async Task<PartialQueryResult<TaskCardListItemDto>> GetAllTaskCardsList([FromQuery] GetAllTaskCardsListQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<TaskCardListItemDto>>(query, cancellationToken);
    }

    [HttpGet("current_instrument")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<IEnumerable<TaskCardDto>> GetTaskCardsForCurrentInstrument()
    {
        var instrumentNumber = _currentInstrumentService.InstrumentNumber;
        if (instrumentNumber != null)
        {
            return await _sender.Send(new GetTaskCardsQuery(instrumentNumber));
        }

        return new List<TaskCardDto>();
    }

    [HttpGet("taskcardinformation")]
    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public async Task<TaskCardInformationDto> GetTaskCardsInformation()
    {
        var instrumentNumber = _currentInstrumentService.InstrumentNumber;
        if (instrumentNumber != null)
        {
            return await _sender.Send(new GetTaskCardInformationQuery(instrumentNumber));
        }

        return new TaskCardInformationDto();
    }

    [HttpGet("lockscreen")]
    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public async Task<IEnumerable<TaskCardDto>> GetTaskCardsForLockScreenAsync()
    {
        var instrumentNumber = _currentInstrumentService.InstrumentNumber;
        if (instrumentNumber != null)
        {
            return await _sender.Send(new GetTaskCardsForLockScreenQuery(instrumentNumber));
        }

        return new List<TaskCardDto>();
    }
}