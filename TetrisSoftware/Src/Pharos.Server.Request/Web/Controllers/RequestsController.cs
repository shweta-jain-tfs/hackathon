using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Command;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Request.Application.Commands;
using Pharos.Server.Request.Application.Commands.CreateUpdateRequest;
using Pharos.Server.Request.Application.Commands.DeleteRequest;
using Pharos.Server.Request.Application.Commands.ResetTestOrders;
using Pharos.Server.Request.Application.Mapping;
using Pharos.Server.Request.Application.Queries;
using Pharos.Server.Request.Application.Queries.GetRequestDetails;
using Pharos.Server.Request.Application.Queries.GetRequests;
using Pharos.Server.Request.Application.Queries.GetResettableTestsCount;

namespace Pharos.Server.Request.Web.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController(ISender sender, ICurrentInstrumentService currentInstrument) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public Task<PartialQueryResult<RequestSummaryDto>> GetRequests([FromQuery] GetRequestsQuery query, CancellationToken cancellationToken)
    {
        return sender.Send<PartialQueryResult<RequestSummaryDto>>(query, cancellationToken);
    }

    [HttpGet("{id}/details")]
    [Authorize]
    public Task<RequestDetailsDto> GetRequestDetails(int id, CancellationToken cancellationToken)
    {
        //if this query is invoked from an instrument, pass the serial number to the query
        var instrumentNumber = currentInstrument.InstrumentNumber ?? "";
        return sender.Send(new GetRequestDetailsQuery(id, instrumentNumber), cancellationToken);
    }

    [HttpPost]
    [Authorize(Permissions.ManageTestOrders)]
    public Task<int> CreateRequest(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        return sender.Send(command, cancellationToken);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.ManageTestOrders)]
    public Task UpdateRequest(int id, [FromBody] UpdateRequestCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return sender.Send(command, cancellationToken);
    }

    [HttpDelete]
    [Authorize(Permissions.ManageTestOrders)]
    public Task<DeleteResponse> DeleteRequest(int[] ids, CancellationToken cancellationToken)
    {
        var command = new DeleteRequestCommand(ids);
        return sender.Send(command, cancellationToken);
    }

    [HttpGet("id")]
    [Authorize]
    public Task<RequestIdResponseDto> GetRequestId(string sampleId, CancellationToken cancellationToken)
    {
        return sender.Send(new GetRequestIdQuery(sampleId), cancellationToken);
    }

    [HttpGet("sampleTubeImage")]
    [Authorize(Policies.ServiceEngineerOrRndMode)]
    public async Task<SampleTubeImageDto> GetSampleTubeImage(string instrumentNumber, string imageId, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetSampleTubeImageQuery(instrumentNumber, imageId), cancellationToken);
    }

    [HttpPost("{id}/unloadSampleTubes")]
    [Authorize(Permissions.ManageSamples)]
    public async Task UnloadSampleTubes(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new UnloadSampleTubesForRequestCommand(id), cancellationToken);
    }

    [HttpGet("pendingReflexTests")]
    [Authorize(Permissions.ApproveTestResults)]
    public Task<ICollection<PendingReflexDto>> GetPendingReflexes([FromQuery] IReadOnlyList<Guid> testOrderIds, CancellationToken cancellationToken)
    {
        return sender.Send(new GetPendingReflexesQuery(testOrderIds), cancellationToken);
    }

    [HttpPost("handlePendingReflexTests")]
    [Authorize(Permissions.ApproveTestResults)]
    public async Task HandlePendingReflexes([FromBody] HandlePendingReflexesCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
    }

    [HttpPost("resetTestOrders")]
    [Authorize(Permissions.ManageTestOrders)]
    public async Task ResetTestOrders([FromBody] ResetTestOrdersCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
    }

    [HttpPost("resettableTestsCount")]
    [Authorize(Permissions.ManageTestOrders)]
    public Task<int> GetResettableTestsCount([FromBody] GetResettableTestsCountQuery query, CancellationToken cancellationToken)
    {
        return sender.Send(query, cancellationToken);
    }

    [HttpGet("hasManual")]
    [Authorize]
    public Task<bool> HasManualReflexDefinitionsAsync([FromQuery] int analyticalRunId, CancellationToken cancellationToken)
    {
        return sender.Send(new HasManualReflexQuery(analyticalRunId), cancellationToken);
    }

    [HttpPost("exportRequestsToLis")]
    [Authorize(Permissions.ManageTestOrders)]
    public async Task ExportRequestsToLisAsync([FromBody] List<int> requestIds, CancellationToken cancellationToken)
    {
        await sender.Send(new ExportRequestsToLisCommand(requestIds), cancellationToken);
    }

    [HttpGet("{id}/tubeHistory")]
    [Authorize]
    public Task<List<SampleTubeHistoryDto>> GetSampleTubeHistoryById(int id, CancellationToken cancellationToken)
    {
        return sender.Send(new GetSampleTubeHistoryByIdQuery(id), cancellationToken);
    }

    [HttpPost("neededSampleVolume")]
    [Authorize]
    public async Task<VolumeNeededDto> GetNeededSampleVolume([FromBody] List<VolumeCalculationTestOrder> items, CancellationToken cancellationToken)
    {
        var query = new GetNeededSampleVolumeQuery(string.Empty, items.Select(TestOrderMapper.ToInstrumentTestOrder).ToList());

        var response = await sender.Send(query, cancellationToken);

        var last = response?.NeededSampleVolumes.LastOrDefault();

        return new VolumeNeededDto(last?.NeededSampleVolumeAccumulated);
    }

    public record VolumeNeededDto(int? AccumulatedVolumeNeeded);

    public record VolumeCalculationTestOrder
    (
        Guid TestOrderId,
        string MethodName,
        string TestName,
        int NumberOfReplicates,
        int InstrumentDilutionFactor,
        int ExecutionOrder
    );
}