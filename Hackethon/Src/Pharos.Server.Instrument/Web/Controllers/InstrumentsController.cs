using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Instrument.Application.Commands.DeleteInstrument;
using Pharos.Server.Instrument.Application.Commands.UpdateInstrument;
using Pharos.Server.Instrument.Application.Commands.UpdateInstrumentTechnologies;
using Pharos.Server.Instrument.Application.Queries.GetInstruments;
using Pharos.Server.Instrument.Application.Queries.GetInstrumentTechnologies;

namespace Pharos.Server.Instrument.Web.Controllers;

[ApiController]
[Route("api/instruments")]
public class InstrumentsController(ISender sender) : ControllerBase
{
    [HttpGet("all")]
    [Authorize]
    public async Task<IEnumerable<InstrumentDto>> GetAll(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetInstrumentsQuery(), cancellationToken);
    }

    [HttpGet("{id}")]
    [Authorize]
    public Task<InstrumentDto> GetInstrument(int id, CancellationToken cancellationToken)
    {
        return sender.Send(new GetInstrumentQuery(id), cancellationToken);
    }

    [HttpDelete("{id}")]
    [Authorize(Permissions.ManageInstruments)]
    [Authorize(Policies.ServiceEngineerOrRndMode)]
    public Task DeleteInstrument(int id, CancellationToken cancellationToken)
    {
        var command = new DeleteInstrumentCommand(id);
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("{id}")]
    [Authorize(Permissions.ManageInstruments)]
    public Task UpdateInstrument(int id, [FromBody] UpdateInstrumentCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return sender.Send(command, cancellationToken);
    }

    [HttpGet("{id}/technologies")]
    [Authorize]
    public Task<List<InstrumentTechnologyDto>> GetInstrumentTechnologies(int id, CancellationToken cancellationToken)
    {
        return sender.Send(new GetInstrumentTechnologiesQuery(id), cancellationToken);
    }

    [HttpPut("{id}/technologies")]
    [Authorize(Permissions.ManageInstruments)]
    public Task UpdateInstrumentTechnologies(int id, [FromBody] UpdateInstrumentTechnologiesCommand command, CancellationToken cancellationToken)
    {
        command.InstrumentId = id;
        return sender.Send(command, cancellationToken);
    }
}
