using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Sample.Application.Commands.ClearSampleTrays;
using Pharos.Server.Sample.Application.Commands.CreateSampleTray;
using Pharos.Server.Sample.Application.Commands.DeleteSampleTray;
using Pharos.Server.Sample.Application.Commands.SetTrayNotInInstrument;
using Pharos.Server.Sample.Application.Queries.GetSampleTrayList;

namespace Pharos.Server.Sample.Web.Controllers;

[ApiController]
[Route("api/trays")]
public class SampleTrayController : ControllerBase
{
    private readonly ISender _sender;

    public SampleTrayController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize]
    public Task<PartialQueryResult<SampleTrayDto>> GetAll([FromQuery] GetSampleTrayListQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<SampleTrayDto>>(query, cancellationToken);
    }

    [HttpGet("{id}")]
    [Authorize]
    public Task<SampleTrayDto> GetSampleTray(string id, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetSampleTrayQuery(id), cancellationToken);
    }

    [HttpPost]
    [Authorize(Permissions.EditSystemSettings)]
    public Task<string> CreateSampleTray(CreateSampleTrayCommand command, CancellationToken cancellationToken)
    {
        return _sender.Send(command, cancellationToken);
    }

    [HttpPost("clear")]
    [Authorize(Permissions.ManageSamples)]
    public Task<List<string>> ClearSampleTrays(string[] ids, CancellationToken cancellationToken)
    {
        var command = new ClearSampleTraysCommand(ids);
        return _sender.Send(command, cancellationToken);
    }

    [HttpDelete]
    [Authorize(Permissions.EditSystemSettings)]
    public Task<List<string>> DeleteSampleTray(string[] ids, CancellationToken cancellationToken)
    {
        var command = new DeleteSampleTrayCommand(ids);
        return _sender.Send(command, cancellationToken);
    }

    [HttpPost("{id}/setNotInInstrument")]
    [Authorize(Permissions.ManageSamples)]
    public Task SetTrayNotInInstrument(string id, CancellationToken cancellationToken)
    {
        return _sender.Send(new SetTrayNotInInstrumentCommand(id), cancellationToken);
    }
}