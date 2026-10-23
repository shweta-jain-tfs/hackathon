using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Instrument.Application.Commands.SetAllAssaySchedulersActive;
using Pharos.Server.Instrument.Application.Commands.UpdateAssayScheduler;
using Pharos.Server.Instrument.Application.Commands.UpdateAssaySchedulerDay;
using Pharos.Server.Instrument.Application.Queries.GetAssaySchedulers;

namespace Pharos.Server.Instrument.Web.Controllers;

[ApiController]
[Route("api/assay-scheduler")]
public class AssaySchedulerController(ISender sender) : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize]
    public Task<AssaySchedulerDto> GetById(int id, CancellationToken cancellationToken)
    {
        return sender.Send(new GetAssaySchedulerQuery(id), cancellationToken);
    }

    /// <summary>
    /// Get the assay scheduler for all instruments.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize]
    public async Task<IEnumerable<AssaySchedulerDto>> GetAssaySchedulers(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetAssaySchedulersQuery(), cancellationToken);
    }

    /// <summary>
    /// Set all assay schedulers to active/inactive.
    /// </summary>
    [HttpPut("set-all-active")]
    [Authorize(Permissions.ManageInstruments)]
    public async Task SetAllActive([FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        await sender.Send(new SetAllAssaySchedulersActiveCommand(isActive), cancellationToken);
    }

    /// <summary>
    /// Update the whole scheduler for an instrument.
    /// </summary>
    /// <param name="instrumentId"></param>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{instrumentId:int}")]
    [Authorize(Permissions.ManageInstruments)]
    public Task UpdateAssayScheduler(int instrumentId, [FromBody] UpdateAssaySchedulerCommand command, CancellationToken cancellationToken)
    {
        command.InstrumentId = instrumentId;
        return sender.Send(command, cancellationToken);
    }

    /// <summary>
    /// Update a specific day in the assay scheduler for an instrument.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("day/{id:int}")]
    [Authorize(Permissions.ManageInstruments)]
    public Task UpdateAssaySchedulerDay(int id, [FromBody] UpdateAssaySchedulerDayCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return sender.Send(command, cancellationToken);
    }
}