using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.SystemUpdate.Application.Commands;
using Pharos.Server.SystemUpdate.Application.Queries;
using Pharos.Server.SystemUpdate.Application.Queries.GetUnacknowledgedUpdateOutcome;

namespace Pharos.Server.SystemUpdate.Web;

[ApiController]
[Route("api/systemupdate")]
public sealed class SystemUpdateController(ISender sender) : ControllerBase
{
    [HttpGet("availableUpdates")]
    [Authorize]
    public Task<List<string>> GetAvailableUpdates(CancellationToken cancellationToken)
    {
        return sender.Send(new GetAvailableUpdatesQuery(), cancellationToken);
    }

    [HttpPost("initiateUpdate")]
    [Authorize(Permissions.ManageInstruments)]
    [Authorize(Permissions.EditSystemSettings)]
    public Task InitiateUpdate(CancellationToken cancellationToken)
    {
        return sender.Send(new InitiateSystemUpdateCommand(), cancellationToken);
    }

    [HttpPost("acknowledgeUpdate")]
    [Authorize(Permissions.ManageInstruments)]
    [Authorize(Permissions.EditSystemSettings)]
    public Task AcknowledgeUpdate(CancellationToken cancellationToken)
    {
        return sender.Send(new AcknowledgeUpdateCommand(), cancellationToken);
    }

    [HttpGet("unacknowledgedOutcome")]
    [Authorize]
    public Task<SystemUpdateOutcomeDto> GetUnacknowledgedOutcome(CancellationToken cancellationToken)
    {
        return sender.Send(new GetUnacknowledgedUpdateOutcomeQuery(), cancellationToken);
    }
}