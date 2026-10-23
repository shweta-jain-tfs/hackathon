using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Command;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Commands.CreateRequestor;
using Pharos.Server.Request.Application.Commands.DeleteRequestor;
using Pharos.Server.Request.Application.Commands.UpdateRequestor;
using Pharos.Server.Request.Application.Queries.GetRequestor;
using Pharos.Server.Request.Application.Queries.GetRequestorDetails;
using Pharos.Server.Request.Application.Queries.GetRequestors;

namespace Pharos.Server.Request.Web.Controllers;

[ApiController]
[Route("api/requestors")]
public class RequestorsController(ISender sender, IOptionsSnapshot<RequestSettings> requestSettings) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public async Task<PartialQueryResult<RequestorDto>> GetRequestor([FromQuery] GetRequestorQuery query, CancellationToken cancellationToken)
    {
        if (!requestSettings.Value.IsRequestorInformationEnabled)
        {
            // Return empty result if requestor info is disabled.
            return new PartialQueryResult<RequestorDto>([], 0, 0);
        }

        return await sender.Send<PartialQueryResult<RequestorDto>>(query, cancellationToken);
    }

    [HttpGet("byRequestorId")]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public async Task<RequestorDto?> GetRequestorByRequestId([FromQuery] GetRequestorByRequestIdQuery query, CancellationToken cancellationToken)
    {
        if (!requestSettings.Value.IsRequestorInformationEnabled)
        {
            // Return empty result if requestor info is disabled.
            return null;
        }

        return await sender.Send(query, cancellationToken);
    }

    [HttpGet("requestors")]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public async Task<PartialQueryResult<RequestorSummaryDto>> GetRequestors([FromQuery] GetRequestorsQuery query, CancellationToken cancellationToken)
    {
        if (!requestSettings.Value.IsRequestorInformationEnabled)
        {
            // Return empty result if requestor info is disabled.
            return new PartialQueryResult<RequestorSummaryDto>([], 0, 0);
        }

        return await sender.Send<PartialQueryResult<RequestorSummaryDto>>(query, cancellationToken);
    }

    [HttpGet("{id}")]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public Task<RequestorDetailsDto?> GetRequestorDetails(int id, CancellationToken cancellationToken)
    {
        if (!requestSettings.Value.IsRequestorInformationEnabled)
        {
            // Return null if requestor info is disabled.
            return Task.FromResult<RequestorDetailsDto?>(null);
        }

        return sender.Send(new GetRequestorDetailsQuery(id), cancellationToken);
    }

    [HttpDelete]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public Task<DeleteResponse> DeleteRequestors(List<int> ids, CancellationToken cancellationToken)
    {
        if (!requestSettings.Value.IsRequestorInformationEnabled)
        {
            // Return no-op response if requestor info is disabled.
            return Task.FromResult(new DeleteResponse { Deleted = 0, Skipped = ids.Count });
        }

        return sender.Send(new DeleteRequestorCommand(ids), cancellationToken);
    }

    [HttpPost]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public Task<int> CreateRequestor(CreateRequestorCommand command, CancellationToken cancellationToken)
    {
        if (!requestSettings.Value.IsRequestorInformationEnabled)
        {
            // Return 0 if requestor info is disabled.
            return Task.FromResult(0);
        }

        return sender.Send(command, cancellationToken);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public Task UpdateRequestor(int id, [FromBody] UpdateRequestorCommand command, CancellationToken cancellationToken)
    {
        if (!requestSettings.Value.IsRequestorInformationEnabled)
        {
            // Do nothing if requestor info is disabled.
            return Task.CompletedTask;
        }

        command.Id = id;
        return sender.Send(command, cancellationToken);
    }

    [HttpPut("resetCounters")]
    [Authorize(Permissions.ViewPersonalAndHealthData)]
    public Task ResetCounters(int id, CancellationToken cancellationToken)
    {
        return sender.Send(new ResetRequestorCountersCommand(id), cancellationToken);
    }
}