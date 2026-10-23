using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Sample.Application.Commands.UpdateSampleTrayPosition;
using Pharos.Server.Sample.Application.Queries;
using Pharos.Server.Sample.Application.Queries.GetSampleTrayDetails;

namespace Pharos.Server.Sample.Web.Controllers;

[ApiController]
[Route("api/trays")]
public class SampleTrayDetailsController : ControllerBase
{
    private readonly ISender _sender;

    public SampleTrayDetailsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}/details")]
    [Authorize]
    public async Task<SampleTrayDetailsDto> GetSampleTrayDetails(string id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetSampleTrayDetailsQuery(id), cancellationToken);
    }

    [HttpPost("{id}/tube-position")]
    [Authorize(Permissions.ManageSamples)]
    public Task UpdateSampleTrayPosition(string id, UpdateSampleTrayPositionCommand command, CancellationToken cancellationToken)
    {
        command.TrayId = id;
        return _sender.Send(command, cancellationToken);
    }

    [HttpGet("{id}/location")]
    [Authorize(Permissions.ManageSamples)]
    public Task<string> GetSampleTrayLocation(string id, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetSampleTrayLocationQuery(id), cancellationToken);
    }
}