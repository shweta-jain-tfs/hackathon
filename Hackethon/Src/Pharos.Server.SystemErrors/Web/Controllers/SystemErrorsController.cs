using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.SystemErrors.Application.Commands;
using Pharos.Server.SystemErrors.Application.Queries.GetSystemError;
using Pharos.Server.SystemErrors.Application.Queries.GetSystemErrorDetails;
using Pharos.Server.SystemErrors.Application.Queries.GetSystemErrors;

namespace Pharos.Server.SystemErrors.Web.Controllers;

[ApiController]
[Route("api/systemerrors")]
public class SystemErrorsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentInstrumentService _currentInstrument;

    public SystemErrorsController(ISender sender, ICurrentInstrumentService currentInstrument)
    {
        _sender = sender;
        _currentInstrument = currentInstrument;
    }

    [HttpGet]
    [Authorize]
    public async Task<PartialQueryResult<SystemErrorSummaryDto>> GetSystemErrors([FromQuery] GetSystemErrorsQuery query, CancellationToken cancellationToken)
    {
        return await _sender.Send<PartialQueryResult<SystemErrorSummaryDto>>(query, cancellationToken);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<SystemErrorSummaryDto> GetSystemError(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetSystemErrorQuery(id), cancellationToken);
    }

    [HttpPost]
    [Authorize(Permissions.ManageInstruments)]
    public async Task<DateTimeOffset> RespondToError(RespondToErrorCommand command, CancellationToken cancellationToken)
    {
        return await _sender.Send(command, cancellationToken);
    }

    [HttpGet("{id}/details")]
    [Authorize]
    public async Task<SystemErrorDetailsDto> GetSystemErrorDetails(int id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetSystemErrorDetailsQuery(id), cancellationToken);
    }

    [HttpGet("GetFatalErrors")]
    [Authorize(Policies.InstrumentOnly)]
    public async Task<List<int>> GetFatalSystemErrors(CancellationToken cancellationToken)
    {
        var serialNumber = _currentInstrument.InstrumentNumber ?? throw new NotFoundException();

        return await _sender.Send(new GetFatalSystemErrorsQuery(serialNumber), cancellationToken);
    }

    [HttpPost("acknowledge")]
    [Authorize(Permissions.ManageInstruments)]
    public async Task AcknowledgeSystemErrors(AcknowledgeSystemErrorsCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
    }
}