using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Sample.Application.Commands;

namespace Pharos.Server.Sample.Web.Controllers;

[ApiController]
[Route("api/tubes")]
public class TubesController : ControllerBase
{
    private readonly ISender _sender;

    public TubesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("unloadQualityControl")]
    [Authorize(Permissions.ManageSamples)]
    public async Task UnloadQualityControlTubes(UnloadQualityControlTubesCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }
}