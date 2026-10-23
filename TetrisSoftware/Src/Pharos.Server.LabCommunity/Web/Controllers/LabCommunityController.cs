using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Server.LabCommunity.Application.Queries;

namespace Pharos.Server.LabCommunity.Web.Controllers;

[ApiController]
[Route("api/labcommunity")]
public class LabCommunityController(ISender sender) : ControllerBase
{
    [HttpGet("status")]
    [Authorize]
    public async Task<LabCommunityConnectionStatusDto> GetStatus(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetLabCommunityConnectionStatusQuery(), cancellationToken);
    }

    [HttpGet("enabled")]
    [Authorize]
    public async Task<bool> IsLabCommunityEnabled(CancellationToken cancellationToken)
    {
        return await sender.Send(new IsLabCommunityEnabledQuery(), cancellationToken);
    }
}
