using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Command;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Request;
using Pharos.Server.Parameters.Application.Commands.Panels;
using Pharos.Server.Parameters.Application.Queries.GetMethodPanelDetails;
using Pharos.Server.Parameters.Application.Queries.GetMethodPanels;
using Pharos.Server.Parameters.Application.Queries.GetPanels;

namespace Pharos.Server.Parameters.Web.Controllers;

[ApiController]
[Route("api/panels")]
public class PanelsController : ControllerBase
{
    private readonly ISender _sender;

    public PanelsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("panels")]
    [Authorize]
    public Task<PartialQueryResult<PanelDto>> GetPanels([FromQuery] GetPanelsQuery query, CancellationToken cancellationToken)
    {
        return _sender.Send<PartialQueryResult<PanelDto>>(query, cancellationToken);
    }

    [HttpGet("methodPanels")]
    [Authorize]
    public Task<List<MethodPanelDto>> GetMethodPanels(CancellationToken cancellationToken)
    {
        return _sender.Send(new GetMethodPanelsQuery(), cancellationToken);
    }

    [HttpGet("methodPanels/checkReferenced")]
    [Authorize(Permissions.EditSystemSettings)]
    public Task<bool> CheckMethodPanelsReferenced([FromQuery] int[] ids, CancellationToken cancellationToken)
    {
        return _sender.Send(new CheckTestPanelsReferencedQuery(ids), cancellationToken);
    }

    [HttpGet("methodPanels/{id}")]
    [Authorize]
    public Task<MethodPanelDetailsDto> GetMethodPanelDetails(int id, CancellationToken cancellationToken)
    {
        return _sender.Send(new GetMethodPanelDetailsQuery(id), cancellationToken);
    }

    [HttpPost("methodPanels")]
    [Authorize(Permissions.EditSystemSettings)]
    public Task<int> CreateMethodPanel(CreatePanelCommand command, CancellationToken cancellationToken)
    {
        return _sender.Send(command, cancellationToken);
    }

    [HttpPut("methodPanels/{id}")]
    [Authorize(Permissions.EditSystemSettings)]
    public Task UpdateMethodPanel(int id, [FromBody] UpdatePanelCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return _sender.Send(command, cancellationToken);
    }

    [HttpDelete("methodPanels")]
    [Authorize(Permissions.EditSystemSettings)]
    public Task<DeleteResponse> DeleteMethodPanels(DeletePanelsCommand command, CancellationToken cancellationToken)
    {
        return _sender.Send(command, cancellationToken);
    }
}
